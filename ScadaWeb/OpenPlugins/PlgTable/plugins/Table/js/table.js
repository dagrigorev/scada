// Column header time format options
const HEADER_TIME_OPTIONS = { hour: '2-digit', minute: '2-digit' }
// Column header date and time format options
const HEADER_DATETIME_OPTIONS = { month: 'short', day: '2-digit', hour: '2-digit', minute: '2-digit' }

// Input channel filter for current and hourly data requests
let cnlFilter = null
// Hour period: firstHour <= hourPeriod.startHour <= hourPeriod.endHour <= lastHour
let hourPeriod = null
// First possible hour of the hour period
let firstHour = null
// Last possible hour of the hour period
let lastHour = null

// jQuery cells those display current data
let curDataCells = null
// Array of columns those display hourly data, and which consist of jQuery cells
const hourDataCols = []
// Received hourly data age
let dataAge = []
// Timeout ID of the hourly data updating timer
let updateHourDataTimeoutID = null

// View title. Must be defined in Table.aspx
var viewTitle = viewTitle || ''

// Set window title
function setTitle () {
  if (viewTitle) {
    document.title = viewTitle + ' - Rapid SCADA'
    if (viewHub) {
      viewHub.notify(scada.EventTypes.VIEW_TITLE_CHANGED, window, document.title)
    }
  }
}

// Set current view date and process the consequent changes
function changeViewDate (date, notify) {
  setViewDate(date)
  createHourPeriod()
  updateHourDataColHdrText()
  restartUpdatingHourData()

  if (notify) {
    sendViewDateNotification(date)
  }
}

// Initialize first and last possible hours of the time period
function initHourLimits () {
  firstHour = $('#divTblWrapper tr:first td.hour:first').data('hour')
  lastHour = $('#divTblWrapper tr:first td.hour:last').data('hour')
}

// Create the hour period according to the view date and control values
function createHourPeriod () {
  hourPeriod = new scada.HourPeriod()
  hourPeriod.date = viewDate
  hourPeriod.startHour = parseInt($('#selTimeFrom').val())
  hourPeriod.endHour = parseInt($('#selTimeTo').val())
  dataAge = [] // reset received hourly data age
}

// Correct the beginning of the hour period
function correctStartHour () {
  if (hourPeriod.startHour > hourPeriod.endHour) {
    hourPeriod.startHour = hourPeriod.endHour
    $('#selTimeFrom').val(hourPeriod.startHour)
  }
}

// Correct the end of the hour period
function correctEndHour () {
  if (hourPeriod.endHour < hourPeriod.startHour) {
    hourPeriod.endHour = hourPeriod.startHour
    $('#selTimeTo').val(hourPeriod.endHour)
  }
}

// Save the hour period in the cookies
function saveHourPeriod () {
  scada.utils.setCookie('Table.TimeFrom', $('#selTimeFrom').val())
  scada.utils.setCookie('Table.TimeTo', $('#selTimeTo').val())
}

// Export hour data to Excel
function exportHourData () {
  const exportUrl = 'HourDataRepOut.aspx?viewID=' + viewID +
        '&' + scada.utils.dateToQueryString(viewDate) +
        '&startHour=' + hourPeriod.startHour + '&endHour=' + hourPeriod.endHour
  window.location = exportUrl
}

// Select and prepare the current data cells
function initCurDataCells () {
  // select cells
  curDataCells = $('#divTblWrapper td.cur')

  // copy data-cnl attributes from rows to the cells
  curDataCells.each(function () {
    $(this).attr('data-cnl', $(this).closest('tr.item').data('cnl'))
  })
}

// Select and prepare the hourly data columns
function initHourDataCols () {
  // init columns
  hourDataCols.length = lastHour - firstHour + 1
  for (let i = 0, len = hourDataCols.length; i < len; i++) {
    hourDataCols[i] = []
  }

  // get and arrange cells
  // Note: cell.attr() significantly faster than cell.data()
  $('#divTblWrapper tr.item').each(function () {
    const row = $(this)
    const cnlNum = row.data('cnl')
    row.find('td.hour').each(function () {
      const cell = $(this)
      cell.attr('data-cnl', cnlNum)
      const hour = cell.attr('data-hour') // faster than cell.data("hour")
      hourDataCols[hour - firstHour].push(cell)
    })
  })
}

// Update header text of the hourly data columns
function updateHourDataColHdrText () {
  // converting date to string doesn't work properly on iOS
  if (!scada.utils.iOS()) {
    $('#divTblWrapper tr.hdr td.hour').each(function () {
      const cell = $(this)
      const hour = cell.data('hour')
      const colDT = new Date(viewDate.getTime())
      colDT.setHours(hour)

      const hdrText = hourPeriod.startHour >= 0
        ? colDT.toLocaleTimeString(locale, HEADER_TIME_OPTIONS)
        : /* display time only */ colDT.toLocaleString(locale, HEADER_DATETIME_OPTIONS) /* display date and time */
      cell.children('span').text(hdrText)
    })
  }
}

// Update visibility of the table view columns according to the hour period
function updateHourDataColVisibility () {
  $('#divTblWrapper tr').each(function () {
    const row = $(this)
    const hourCells = row.find('td.hour')

    // show all the cells of the row
    hourCells.removeClass('hidden')

    // hide cells from the left
    let cellsToHide = hourPeriod.startHour - firstHour
    if (cellsToHide > 0) {
      var cells = hourCells.slice(0, cellsToHide)
      cells.addClass('hidden')
      if (row.hasClass('item')) {
        cells.text('') // clear cell text
      }
    }

    // hide cells from the right
    cellsToHide = lastHour - hourPeriod.endHour
    if (cellsToHide > 0) {
      cells = hourCells.slice(-cellsToHide)
      cells.addClass('hidden')
      if (row.hasClass('item')) {
        cells.text('') // clear cell text
      }
    }
  })
}

// Tune style of the item links to fill cells
function styleItemLinks () {
  $('#divTblWrapper td.cap span.cmd').prev('a.lbl').addClass('before-cmd')
}

// Show hint associated with the icon
function showHintByIcon (imgIcon) {
  const iconOffset = imgIcon.offset()
  const hint = imgIcon.siblings('span.hint')
  let hintTop = iconOffset.top + imgIcon.height()
  const hintHeight = hint.outerHeight()

  if (hintTop + hintHeight > $(document).height()) {
    hintTop = iconOffset.top - hintHeight
    if (hintTop < 0) {
      hintTop = 0
    }
  }

  hint
    .css({
      left: iconOffset.left + imgIcon.outerWidth(true),
      top: hintTop
    })
    .addClass('show')
}

// Hide hint associated with the icon
function hideHintByIcon (imgIcon) {
  hideHint(imgIcon.siblings('span.hint'))
}

// Hide hint associated with the icon
function hideHint (spanHint) {
  spanHint.removeClass('show')
}

// Show chart web page
function showChart (clickedElem) {
  const dialogs = viewHub ? viewHub.dialogs : null
  if (dialogs) {
    const cnlNum = clickedElem.closest('tr.item').data('cnl')
    if (cnlNum) {
      dialogs.showChart(cnlNum, viewID, viewDate)
    }
  } else {
    console.warn(DIALOGS_UNDEFINED)
  }
}

// Show command dialog
function showCmd (clickedElem) {
  const dialogs = viewHub ? viewHub.dialogs : null
  if (dialogs) {
    const ctrlCnlNum = clickedElem.closest('tr.item').data('ctrl')
    dialogs.showCmd(ctrlCnlNum, viewID)
  } else {
    console.warn(DIALOGS_UNDEFINED)
  }
}

// Display the given data in the cell.
// Returns true if the cell text has been changed
function displayCellData (cell, cnlDataMap) {
  const cnlNum = parseInt(cell.attr('data-cnl'))
  if (cnlNum) {
    const cnlData = cnlDataMap.get(cnlNum)
    let text = ''
    let color = ''

    if (cnlData) {
      text = cnlData.Text
      color = cnlData.Color
    }

    const textChanged = cell.text() != text
    cell.text(text) // special characters are encoded
    cell.css('color', color)
    return textChanged
  }
}

// Request and display current data.
// callback is a function (success)
function updateCurData (callback) {
  scada.clientAPI.getCurCnlDataExt(cnlFilter, function (success, cnlDataExtArr) {
    if (success) {
      const cnlDataMap = scada.clientAPI.createCnlDataExtMap(cnlDataExtArr)
      let updateHeader = false

      curDataCells.each(function () {
        const cell = $(this)
        if (displayCellData(cell, cnlDataMap)) {
          updateHeader = true
        }
      })

      if (updateHeader) {
        scada.tableHeader.update()
      }

      callback(true)
    } else {
      callback(false)
    }
  })
}

// Request and display hourly data.
// callback is a function (success)
function updateHourData (callback) {
  const reqHourPeriod = hourPeriod
  const reqDataAge = dataAge

  scada.clientAPI.getHourCnlData(hourPeriod, cnlFilter, scada.HourDataModes.INTEGER_HOURS, reqDataAge,
    function (success, hourCnlDataArr, respDataAge) {
      if (reqHourPeriod != hourPeriod) {
        // do nothing
      } else if (success) {
        const hourCnlDataMap = scada.clientAPI.createHourCnlDataMap(hourCnlDataArr)
        let updateHeader = false

        for (let hour = hourPeriod.startHour; hour <= hourPeriod.endHour; hour++) {
          const hourData = hourCnlDataMap.get(hour)
          const hourCol = hourDataCols[hour - firstHour]

          if (hourData) {
            if (hourData.Modified) {
              var cnlDataMap = scada.clientAPI.createCnlDataExtMap(hourData.CnlDataExtArr)
              $.each(hourCol, function () {
                const cell = $(this)
                if (displayCellData(cell, cnlDataMap)) {
                  updateHeader = true
                }
              })
            }
          } else {
            $.each(hourCol, function () {
              const cell = $(this)
              if (cell.text() != '') {
                updateHeader = true
              }
              cell.text('')
              cell.css('color', '')
            })
            updateHeader = true
          }
        }

        dataAge = respDataAge

        if (updateHeader) {
          scada.tableHeader.update()
        }

        callback(true)
      } else {
        callback(false)
      }
    })
}

// Start cyclic updating current data
function startUpdatingCurData () {
  updateCurData(function (success) {
    if (!success) {
      notifier.addNotification(phrases.UpdateCurDataError, scada.NotifTypes.ERROR, notifier.DEF_NOTIF_LIFETIME)
    }

    setTimeout(startUpdatingCurData, dataRefrRate)
  })
}

// Start cyclic updating hourly data
function startUpdatingHourData () {
  updateHourData(function (success) {
    if (!success) {
      notifier.addNotification(phrases.UpdateHourDataError, scada.NotifTypes.ERROR, notifier.DEF_NOTIF_LIFETIME)
    }

    updateHourDataTimeoutID = setTimeout(startUpdatingHourData, arcRefrRate)
  })
}

// Restart updating hourly data immediately
function restartUpdatingHourData () {
  clearTimeout(updateHourDataTimeoutID)
  startUpdatingHourData()
}

$(document).ready(function () {
  scada.clientAPI.rootPath = '../../'
  scada.clientAPI.ajaxQueue = scada.ajaxQueueLocator.getAjaxQueue()
  setTitle()
  styleIOS()
  updateLayout()
  initViewDate()
  initHourLimits()
  createHourPeriod()
  saveHourPeriod() // update cookie expiration date
  initCurDataCells()
  initHourDataCols()
  updateHourDataColHdrText()
  styleItemLinks()
  scada.tableHeader.create()
  notifier = new scada.Notifier('#divNotif')
  notifier.startClearingNotifications()
  cnlFilter = new scada.CnlFilter()
  cnlFilter.viewID = viewID

  if (DEBUG_MODE) {
    initDebugTools()
  }

  // update layout on window and table area resize
  $(window).on('resize ' + scada.EventTypes.UPDATE_LAYOUT, function () {
    updateLayout()
  })

  // process the view date changing
  $(window).on(scada.EventTypes.VIEW_DATE_CHANGED, function (event, sender, extraParams) {
    changeViewDate(extraParams, false)
  })

  // select view date on click the calendar icon
  $('#spanDate i').click(function (event) {
    selectViewDate(changeViewDate)
  })

  // parse manually entered view date
  $('#txtDate').change(function () {
    parseViewDate($(this).val(), changeViewDate)
  })

  // process the time period changing
  $('#selTimeFrom, #selTimeTo').change(function () {
    createHourPeriod()

    if ($(this).attr('id') == 'selTimeFrom') {
      correctEndHour()
    } else {
      correctStartHour()
    }

    saveHourPeriod()
    updateHourDataColHdrText()
    updateHourDataColVisibility()
    scada.tableHeader.update()
    restartUpdatingHourData()
  })

  // export the table view on the button click
  $('#spanExportBtn').click(function () {
    exportHourData()
  })

  // show and hide hint on hover or touch events
  $('#divTblWrapper img.icon').hover(
    function () {
      showHintByIcon($(this))
    }, function () {
      // timeout prevents label click on tablets
      const icon = $(this)
      setTimeout(function () { hideHintByIcon(icon) }, 100)
    }
  )

  $('#divTblWrapper img.icon').on('touchstart', function () {
    showHintByIcon($(this))
  })

  $('#divTblWrapper span.hint').on('touchend touchcancel', function () {
    const hint = $(this)
    setTimeout(function () { hideHint(hint) }, 100)
  })

  // show chart on a label click
  $('#divTblWrapper a.lbl').click(function () {
    showChart($(this))
    return false
  })

  // send command on a command icon click
  $('#divTblWrapper span.cmd').click(function () {
    showCmd($(this))
  })

  // start updating data
  startUpdatingCurData()
  startUpdatingHourData()
})
