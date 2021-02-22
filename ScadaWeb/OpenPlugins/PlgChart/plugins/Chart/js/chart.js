/*
 * Chart control
 *
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 *
 * Requires:
 * - jquery
 * - utils.js
 */

// Rapid SCADA namespace
var scada = scada || {}
// Chart namespace
scada.chart = scada.chart || {}

/** ******** Constants **********/

// Constants object
scada.chart.const = {
  // Seconds per day
  SEC_PER_DAY: 86400,
  // Milliseconds per day
  MS_PER_DAY: 86400 * 1000
}

/** ******** Display Settings **********/

// Display settings type
scada.chart.DisplaySettings = function () {
  // Application culture name
  this.locale = 'en-GB'
  // Distance between chart points to make a gap
  this.chartGap = 90 / scada.chart.const.SEC_PER_DAY // 90 seconds
}

/** ******** Time Range **********/

// Time range type
scada.chart.TimeRange = function () {
  // Date of the beginning of the range in milliseconds
  this.startDate = 0
  // Left edge of the range, where 0 is 00:00 and 1 is 24:00
  this.startTime = 0
  // Right edge of the range
  this.endTime = 1
}

/** ******** Extended Trend **********/

// Extended trend type
// Note: Casing is caused by C# naming rules
scada.chart.TrendExt = function () {
  // Input channel number
  this.cnlNum = 0
  // Input channel name
  this.cnlName = ''
  // Trend points where each point is array [value, "text", "text with unit", "color"]
  this.trendPoints = []
}

/** ******** Trend Point Indexes **********/

// Trend point indexes enumeration
scada.chart.TrendPointIndexes = {
  VAL_IND: 0,
  TEXT_IND: 1,
  TEXT_WITH_UNIT_IND: 2,
  COLOR_IND: 3
}

/** ******** Chart Data **********/

// Chart data type
scada.chart.ChartData = function () {
  // Time points which number is matched with the number of trend points. Array of numbers
  this.timePoints = []
  // Trends to display. Array of TrendExt
  this.trends = []
  // Name of input channel quantity (and unit)
  this.quantityName = ''
}

/** ******** Chart Layout **********/

// Chart layout type
scada.chart.ChartLayout = function () {
  // Desirable number of horizontal grid lines
  this._GRID_HOR_LINE_CNT = 10

  // Chart left padding
  this.LEFT_PADDING = 10
  // Chart right padding
  this.RIGHT_PADDING = 20
  // Chart top padding
  this.TOP_PADDING = 20
  // Chart bottom padding
  this.BOTTOM_PADDING = 10
  // Tick mark size
  this.TICK_SIZE = 3
  // Data label left and right margins
  this.LBL_LR_MARGIN = 10
  // Data label top and bottom margins
  this.LBL_TB_MARGIN = 5
  // Data labels font
  this.LBL_FONT = '12px Arial'
  // Data labels font size the same as specified above
  this.LBL_FONT_SIZE = 12
  // Line height of various kinds of texts
  this.LINE_HEIGHT = 18
  // Vertical hint offset relative to the cursor
  this.HINT_OFFSET = 20
  // Chart back color
  this.BACK_COLOR = '#ffffff'
  // Default fore color
  this.DEF_COLOR = '#000000'
  // Chart frame color
  this.FRAME_COLOR = '#808080'
  // Grid lines color
  this.GRID_COLOR = '#e0e0e0'
  // Tick marks color
  this.TICK_COLOR = '#808080'
  // Data labels color
  this.LBL_COLOR = '#000000'

  // Chart width
  this.width = 0
  // Chart height
  this.height = 0

  // Grid step for the x-axis
  this.gridXStep = 0
  // Start grid value for the y-axis
  this.gridYStart = 0
  // Grid step for the y-axis
  this.gridYStep = 0
  // Number of decimal places to use in labels for the y-axis
  this.gridYDecDig = 0
  // Max data label width for the y-axis
  this.maxYLblWidth = 0

  // Left coordinate of the drawing area
  this.plotAreaLeft = 0
  // Right coordinate of the drawing area
  this.plotAreaRight = 0
  // Top coordinate of the drawing area
  this.plotAreaTop = 0
  // Bottom coordinate of the drawing area
  this.plotAreaBottom = 0
  // Drawing area width
  this.plotAreaWidth = 0
  // Drawing area height
  this.plotAreaHeight = 0

  // Width of the canvas left border
  this.canvasLeftBorder = 0
  // Width of the canvas top border
  this.canvasTopBorder = 0
  // Absolute left coordinate of the canvas relative to the document
  this.absCanvasLeft = 0
  // Absolute top coordinate of the canvas relative to the document
  this.absCanvasTop = 0
  // Absolute left coordinate of the drawing area relative to the document
  this.absPlotAreaLeft = 0
  // Absolute right coordinate of the drawing area relative to the document
  this.absPlotAreaRight = 0
  // Absolute top coordinate of the drawing area relative to the document
  this.absPlotAreaTop = 0
  // Absolute bottom coordinate of the drawing area relative to the document
  this.absPlotAreaBottom = 0
}

// Calculate grid parameters for the x-axis
scada.chart.ChartLayout.prototype._calcGridX = function (minX, maxX) {
  const cnt = 8
  const ranges = [16, 8, 4, 2, 1, 1 / 2, 1 / 4, 1 / 12] // displayed ranges, days
  const steps = [24, 12, 6, 3, 2, 1, 1 / 2, 1 / 4] // grid steps according to the ranges, hours
  let minStep = 1 / 12 // 5 minutes
  const range = maxX - minX

  for (let i = 0; i < cnt; i++) {
    if (range > ranges[i]) {
      minStep = steps[i]
      break
    }
  }

  this.gridXStep = 1 / 24 * minStep
}

// Calculate grid parameters for the y-axis
scada.chart.ChartLayout.prototype._calcGridY = function (context, minY, maxY) {
  this.gridYStep = (maxY - minY) / this._GRID_HOR_LINE_CNT
  this.gridYDecDig = 0
  let n = 1

  if (this.gridYStep >= 1) {
    while (this.gridYStep > 10) {
      this.gridYStep /= 10
      n *= 10
    }
  } else {
    while (this.gridYStep < 1) {
      this.gridYStep *= 10
      n /= 10
      this.gridYDecDig++
    }
  }

  this.gridYStep = Math.floor(this.gridYStep)

  // the first significant digit of the grid step is 1, 2 or 5
  if (this.gridYStep >= 3 && this.gridYStep <= 4) {
    this.gridYStep = 2
  } else if (this.gridYStep >= 6 && this.gridYStep <= 9) {
    this.gridYStep = 5
  }

  this.gridYStep *= n
  this.gridYStart = Math.floor(minY / this.gridYStep) * this.gridYStep + this.gridYStep

  // measure max data label width
  let maxWidth = 0
  for (let y = this.gridYStart; y < maxY; y += this.gridYStep) {
    const w = context.measureText(y.toFixed(this.gridYDecDig)).width
    if (maxWidth < w) { maxWidth = w }
  }
  this.maxYLblWidth = maxWidth
}

// Calculate coordinates of the drawing area
scada.chart.ChartLayout.prototype._calcPlotArea = function (canvasJqObj, trendCnt, showDates) {
  this.plotAreaLeft = this.LEFT_PADDING + this.LINE_HEIGHT /* y-axis title */ +
        this.maxYLblWidth + this.LBL_LR_MARGIN * 2
  this.plotAreaRight = this.width - this.RIGHT_PADDING
  this.plotAreaTop = this.TOP_PADDING
  this.plotAreaBottom = this.height - this.BOTTOM_PADDING - this.LBL_TB_MARGIN - this.LINE_HEIGHT /* time labels */ -
         (showDates ? this.LINE_HEIGHT : 0) - this.LBL_TB_MARGIN - trendCnt * this.LINE_HEIGHT
  this.plotAreaWidth = this.plotAreaRight - this.plotAreaLeft + 1
  this.plotAreaHeight = this.plotAreaBottom - this.plotAreaTop + 1

  this.canvasLeftBorder = parseInt(canvasJqObj.css('border-left-width'))
  this.canvasTopBorder = parseInt(canvasJqObj.css('border-top-width'))
  this.updateAbsCoordinates(canvasJqObj)
}

// Calculate chart layout
scada.chart.ChartLayout.prototype.calculate = function (canvasJqObj, context,
  minX, maxX, minY, maxY, trendCnt, showDates) {
  this.width = canvasJqObj.width()
  this.height = canvasJqObj.height()

  this._calcGridX(minX, maxX)
  this._calcGridY(context, minY, maxY)
  this._calcPlotArea(canvasJqObj, trendCnt, showDates)
}

// Update absolute coordinates those depends on canvas offset
scada.chart.ChartLayout.prototype.updateAbsCoordinates = function (canvasJqObj) {
  const offset = canvasJqObj.offset()
  this.absCanvasLeft = offset.left
  this.absCanvasTop = offset.top
  this.absPlotAreaLeft = this.absCanvasLeft + this.canvasLeftBorder + this.plotAreaLeft
  this.absPlotAreaRight = this.absCanvasLeft + this.canvasLeftBorder + this.plotAreaRight
  this.absPlotAreaTop = this.absCanvasTop + this.canvasTopBorder + this.plotAreaTop
  this.absPlotAreaBottom = this.absCanvasTop + this.canvasTopBorder + this.plotAreaBottom
}

// Check if the specified point is located within the chart area
scada.chart.ChartLayout.prototype.pointInPlotArea = function (pageX, pageY) {
  return this.absPlotAreaLeft <= pageX && pageX <= this.absPlotAreaRight &&
        this.absPlotAreaTop <= pageY && pageY <= this.absPlotAreaBottom
}

/** ******** Chart Control **********/

// Chart type
scada.chart.Chart = function (canvasJqObj) {
  // Date format options
  this._DATE_OPTIONS = { month: 'short', day: '2-digit', timeZone: 'UTC' }
  // Time format options
  this._TIME_OPTIONS = { hour: '2-digit', minute: '2-digit', timeZone: 'UTC' }
  // Date and time format options
  this._DATE_TIME_OPTIONS = $.extend({}, this._DATE_OPTIONS, this._TIME_OPTIONS)
  // Colors assigned to trends
  this._TREND_COLORS =
        ['#ff0000' /* Red */, '#0000ff' /* Blue */, '#008000' /* Green */, '#ff00ff' /* Fuchsia */, '#ffa500' /* Orange */,
          '#00ffff' /* Aqua */, '#00ff00' /* Lime */, '#4b0082' /* Indigo */, '#ff1493' /* DeepPink */, '#8b4513']

  // Canvas jQuery object
  this._canvasJqObj = canvasJqObj
  // Canvas where the chart is drawn
  this._canvas = canvasJqObj.length ? canvasJqObj[0] : null
  // Canvas is supported and ready for drawing
  this._canvasOK = this._canvas && this._canvas.getContext
  // Canvas drawing context
  this._context = null
  // Layout of the chart
  this._chartLayout = new scada.chart.ChartLayout()
  // Time mark jQuery object
  this._timeMark = null
  // Trend hint jQuery object
  this._trendHint = null
  // Enable or disable trend hint
  this._hintEnabled = true

  // Left edge of the displayed range
  this._minX = 0
  // Right edge of the displayed range
  this._maxX = 0
  // Bottom edge of the displayed range
  this._minY = 0
  // Top edge of the displayed range
  this._maxY = 0
  // Transformation coefficient of the x-coordinate
  this._coefX = 1
  // Transformation coefficient of the y-coordinate
  this._coefY = 1
  // Show date labels below the x-axis
  this._showDates = false
  // Zoom mode affects calculation of the vertical range
  this._zoomMode = false

  // Display settings
  this.displaySettings = new scada.chart.DisplaySettings()
  // Time range
  this.timeRange = new scada.chart.TimeRange()
  // Chart data
  this.chartData = null
}

// Initialize displayed range according to the chart time range and data
scada.chart.Chart.prototype._initRange = function (opt_reinit) {
  if (this._minX == this._maxX /* not initialized yet */ || opt_reinit) {
    this._minX = Math.min(this.timeRange.startTime, 0)
    this._maxX = Math.max(this.timeRange.endTime, 1)
    this._zoomMode = false
    this._calcYRange()
    this._showDates = this._maxX - this._minX > 1
  }
}

// Claculate top and bottom edges of the displayed range
scada.chart.Chart.prototype._calcYRange = function (opt_startPtInd) {
  // find min and max trend value
  let minY = NaN
  let maxY = NaN
  const minX = this._minX - this.displaySettings.chartGap
  const maxX = this._maxX + this.displaySettings.chartGap

  const timePoints = this.chartData.timePoints
  const startPtInd = opt_startPtInd || 0
  const ptCnt = timePoints.length
  const VAL_IND = scada.chart.TrendPointIndexes.VAL_IND

  for (const trend of this.chartData.trends) {
    const trendPoints = trend.trendPoints

    for (let ptInd = startPtInd; ptInd < ptCnt; ptInd++) {
      const x = timePoints[ptInd]

      if (minX <= x && x <= maxX) {
        const y = trendPoints[ptInd][VAL_IND]
        if (isNaN(minY) || minY > y) {
          minY = y
        }
        if (isNaN(maxY) || maxY < y) {
          maxY = y
        }
      }
    }
  }

  if (isNaN(minY)) {
    minY = -1
    maxY = 1
  } else {
    // calculate extra space
    let extraSpace = minY == maxY ? 1 : (maxY - minY) * 0.05

    // include zero if zoom is off
    const origMinY = minY
    const origMaxY = maxY

    if (!this._zoomMode) {
      if (minY > 0 && maxY > 0) {
        minY = 0
      } else if (minY < 0 && maxY < 0) {
        maxY = 0
      }
      extraSpace = Math.max(extraSpace, (maxY - minY) * 0.05)
    }

    // add extra space
    if (origMinY - minY < extraSpace) {
      minY -= extraSpace
    }
    if (maxY - origMaxY < extraSpace) {
      maxY += extraSpace
    }
  }

  this._minY = minY
  this._maxY = maxY
}

// Check if top and bottom edges are outdated because of new data
scada.chart.Chart.prototype._yRangeIsOutdated = function (startPtInd) {
  const oldMinY = this._minY
  const oldMaxY = this._maxY
  this._calcYRange(startPtInd)
  const outdated = this._minY < oldMinY || this._maxY > oldMaxY

  // restore the range
  this._minY = oldMinY
  this._maxY = oldMaxY

  return outdated
}

// Convert trend x-coordinate to the chart x-coordinate
scada.chart.Chart.prototype._trendXToChartX = function (x) {
  return Math.round((x - this._minX) * this._coefX + this._chartLayout.plotAreaLeft)
}

// Convert trend y-coordinate to the chart y-coordinate
scada.chart.Chart.prototype._trendYToChartY = function (y) {
  return Math.round((this._maxY - y) * this._coefY + this._chartLayout.plotAreaTop)
}

// Convert trend x-coordinate to the page x-coordinate
scada.chart.Chart.prototype._trendXToPageX = function (x) {
  return Math.round((x - this._minX) * this._coefX + this._chartLayout.absPlotAreaLeft)
}

// Convert chart x-coordinate to the trend x-coordinate
scada.chart.Chart.prototype._pageXToTrendX = function (pageX) {
  return (pageX - this._chartLayout.absPlotAreaLeft) / this._coefX + this._minX
},

// Convert trend x-coordinate to the date object
scada.chart.Chart.prototype._trendXToDate = function (x) {
  return new Date(this.timeRange.startDate + Math.round(x * scada.chart.const.MS_PER_DAY))
}

// Get index of the point nearest to the specified page x-coordinate
scada.chart.Chart.prototype._getPointIndex = function (pageX) {
  const timePoints = this.chartData.timePoints
  const ptCnt = timePoints.length

  if (ptCnt < 1) {
    return -1
  } else {
    const x = this._pageXToTrendX(pageX)
    let ptInd = 0

    if (ptCnt == 1) {
      ptInd = 0
    } else {
      // binary search
      let iL = 0
      let iR = ptCnt - 1

      if (x < timePoints[iL] || x > timePoints[iR]) { return -1 }

      while (iR - iL > 1) {
        const iM = Math.floor((iR + iL) / 2)
        const xM = timePoints[iM]

        if (xM == x) { return iM } else if (xM < x) { iL = iM } else { iR = iM }
      }

      ptInd = x - timePoints[iL] < timePoints[iR] - x ? iL : iR
    }

    return Math.abs(x - timePoints[ptInd]) <= this.displaySettings.chartGap ? ptInd : -1
  }
}

// Correct left and right edges of the displayed range to align to the grid
scada.chart.Chart.prototype._alignToGridX = function () {
  const gridXStep = this._chartLayout.gridXStep
  this._minX = Math.floor(this._minX / gridXStep) * gridXStep
  this._maxX = Math.ceil(this._maxX / gridXStep) * gridXStep
}

// Convert x-coordinate that means time into a date and time string
scada.chart.Chart.prototype._dateTimeToStr = function (t) {
  const dateTime = this._trendXToDate(t)
  if (scada.utils.iOS()) {
    const date = new Date(dateTime.getTime())
    date.setUTCMinutes(date.getUTCMinutes() + date.getTimezoneOffset())
    return date.toLocaleDateString(this.displaySettings.locale, this._DATE_OPTIONS) + ', ' +
            this._simpleTimeToStr(dateTime)
  } else {
    return dateTime.toLocaleString(this.displaySettings.locale, this._DATE_TIME_OPTIONS)
  }
}

// Convert time to a string using manual transformations
scada.chart.Chart.prototype._simpleTimeToStr = function (time, opt_showSeconds) {
  const min = time.getUTCMinutes()
  let timeStr = time.getUTCHours() + ':' + (min < 10 ? '0' + min : min)

  if (opt_showSeconds) {
    const sec = time.getUTCSeconds()
    timeStr += ':' + (sec < 10 ? '0' + sec : sec)
  }

  return timeStr
}

// Convert x-coordinate that means time into a time string
scada.chart.Chart.prototype._timeToStr = function (t) {
  const time = new Date(Math.round(t * scada.chart.const.MS_PER_DAY))
  return scada.utils.iOS() // iOS requires manual time formatting
    ? this._simpleTimeToStr(time)
    : time.toLocaleTimeString(this.displaySettings.locale, this._TIME_OPTIONS)
}

// Draw pixel on the chart
scada.chart.Chart.prototype._drawPixel = function (x, y, opt_checkBounds) {
  if (opt_checkBounds) {
    // check if the given coordinates are located within the drawing area
    const layout = this._chartLayout
    if (layout.plotAreaLeft <= x && x <= layout.plotAreaRight &&
            layout.plotAreaTop <= y && y <= layout.plotAreaBottom) {
      this._context.fillRect(x, y, 1, 1)
    }
  } else {
    // just draw a pixel
    this._context.fillRect(x, y, 1, 1)
  }
},

// Draw line on the chart
scada.chart.Chart.prototype._drawLine = function (x1, y1, x2, y2, opt_checkBounds) {
  if (opt_checkBounds) {
    const layout = this._chartLayout
    const minX = Math.min(x1, x2)
    const maxX = Math.max(x1, x2)
    const minY = Math.min(y1, y2)
    const maxY = Math.max(y1, y2)

    if (layout.plotAreaLeft <= minX && maxX <= layout.plotAreaRight &&
            layout.plotAreaTop <= minY && maxY <= layout.plotAreaBottom) {
      opt_checkBounds = false // the line is fully inside the drawing area
    } else if (layout.plotAreaLeft > maxX || minX > layout.plotAreaRight ||
            layout.plotAreaTop > maxY || minY > layout.plotAreaBottom) {
      return // the line is outside the drawing area
    }
  }

  const dx = x2 - x1
  const dy = y2 - y1

  if (dx != 0 || dy != 0) {
    if (Math.abs(dx) > Math.abs(dy)) {
      var a = dy / dx
      var b = -a * x1 + y1

      if (dx < 0) {
        const x0 = x1
        x1 = x2
        x2 = x0
      }

      for (var x = x1; x <= x2; x++) {
        var y = Math.round(a * x + b)
        this._drawPixel(x, y, opt_checkBounds)
      }
    } else {
      var a = dx / dy
      var b = -a * y1 + x1

      if (dy < 0) {
        const y0 = y1
        y1 = y2
        y2 = y0
      }

      for (var y = y1; y <= y2; y++) {
        var x = Math.round(a * y + b)
        this._drawPixel(x, y, opt_checkBounds)
      }
    }
  }
}

// Clear the specified rectangle by filling it with the background color
scada.chart.Chart.prototype._clearRect = function (x, y, width, height) {
  this._setColor(this._chartLayout.BACK_COLOR)
  this._context.fillRect(x, y, width, height)
}

// Set current drawing color
scada.chart.Chart.prototype._setColor = function (color) {
  this._context.fillStyle = this._context.strokeStyle =
        color || this._chartLayout.DEF_COLOR
}

// Get color of the trend with the specified index
scada.chart.Chart.prototype._getColorByTrend = function (trendInd) {
  return this._TREND_COLORS[trendInd % this._TREND_COLORS.length]
}

// Draw the chart frame
scada.chart.Chart.prototype._drawFrame = function () {
  const layout = this._chartLayout
  const frameL = layout.plotAreaLeft - 1
  const frameR = layout.plotAreaRight + 1
  const frameT = layout.plotAreaTop - 1
  const frameB = layout.plotAreaBottom + 1

  this._setColor(layout.FRAME_COLOR)
  this._drawLine(frameL, frameT, frameL, frameB)
  this._drawLine(frameR, frameT, frameR, frameB)
  this._drawLine(frameL, frameT, frameR, frameT)
  this._drawLine(frameL, frameB, frameR, frameB)
}

// Draw chart grid of the x-axis
scada.chart.Chart.prototype._drawGridX = function () {
  const layout = this._chartLayout
  this._context.textAlign = 'center'
  this._context.textBaseline = 'middle'

  let prevLblX = NaN
  let prevLblHalfW = NaN
  const frameB = layout.plotAreaBottom + 1
  const tickT = frameB + 1
  const tickB = frameB + layout.TICK_SIZE
  const lblY = layout.plotAreaBottom + layout.LBL_TB_MARGIN + layout.LINE_HEIGHT / 2
  const lblDateY = lblY + layout.LINE_HEIGHT
  const dayBegTimeText = this._timeToStr(0)

  for (let x = this._minX; x <= this._maxX; x += layout.gridXStep) {
    const ptX = this._trendXToChartX(x)

    // vertical grid line
    this._setColor(layout.GRID_COLOR)
    this._drawLine(ptX, layout.plotAreaTop, ptX, layout.plotAreaBottom)

    // tick
    this._setColor(layout.TICK_COLOR)
    this._drawLine(ptX, tickT, ptX, tickB)

    // label
    this._setColor(layout.LBL_COLOR)
    const lblX = ptX
    const timeText = this._timeToStr(x)
    const lblHalfW = this._context.measureText(timeText).width / 2

    if (isNaN(prevLblX) || lblX - lblHalfW > prevLblX + prevLblHalfW + layout.LBL_LR_MARGIN) {
      this._context.fillText(timeText, lblX, lblY)
      if (this._showDates && timeText == dayBegTimeText) {
        this._context.fillText(this.dateToStr(x), lblX, lblDateY)
      }
      prevLblX = lblX
      prevLblHalfW = lblHalfW
    }
  }
}

// Draw chart grid of the y-axis
scada.chart.Chart.prototype._drawGridY = function () {
  const layout = this._chartLayout
  this._context.textAlign = 'right'
  this._context.textBaseline = 'middle'

  let prevLblY = NaN
  const frameL = layout.plotAreaLeft - 1
  const tickL = frameL - layout.TICK_SIZE
  const tickR = frameL - 1
  const lblX = frameL - layout.LBL_LR_MARGIN

  for (let y = layout.gridYStart; y < this._maxY; y += layout.gridYStep) {
    const ptY = this._trendYToChartY(y)

    // horizontal grid line
    this._setColor(layout.GRID_COLOR)
    this._drawLine(layout.plotAreaLeft, ptY, layout.plotAreaRight, ptY)

    // tick
    this._setColor(layout.TICK_COLOR)
    this._drawLine(tickL, ptY, tickR, ptY)

    // label
    this._setColor(layout.LBL_COLOR)
    const lblY = ptY
    if (isNaN(prevLblY) || prevLblY - lblY > layout.LBL_FONT_SIZE) {
      this._context.fillText(y.toFixed(layout.gridYDecDig), lblX, lblY)
      prevLblY = lblY
    }
  }
}

// Draw the y-axis title
scada.chart.Chart.prototype._drawYAxisTitle = function () {
  if (this.chartData.quantityName) {
    const ctx = this._context
    const layout = this._chartLayout
    ctx.textAlign = 'center'
    ctx.textBaseline = 'middle'
    ctx.save()
    ctx.translate(0, layout.plotAreaBottom)
    ctx.rotate(-Math.PI / 2)
    ctx.fillText(this.chartData.quantityName, layout.plotAreaHeight / 2,
      layout.LEFT_PADDING + layout.LINE_HEIGHT / 2, layout.plotAreaHeight)
    ctx.restore()
  }
}

// Draw lagand that is the input channel names
scada.chart.Chart.prototype._drawLegend = function () {
  const layout = this._chartLayout
  this._context.textAlign = 'left'
  this._context.textBaseline = 'middle'

  const lblX = layout.plotAreaLeft + layout.LBL_FONT_SIZE + layout.LBL_LR_MARGIN
  let lblY = layout.plotAreaBottom + layout.LBL_TB_MARGIN + layout.LINE_HEIGHT /* time labels */ +
        (this._showDates ? layout.LINE_HEIGHT : 0) + layout.LBL_TB_MARGIN + layout.LINE_HEIGHT / 2
  const rectSize = layout.LBL_FONT_SIZE
  const rectX = layout.plotAreaLeft - 0.5
  let rectY = lblY - rectSize / 2 - 0.5
  const trendCnt = this.chartData.trends.length

  for (let trendInd = 0; trendInd < trendCnt; trendInd++) {
    const trend = this.chartData.trends[trendInd]
    const legendText = '[' + trend.cnlNum + '] ' + trend.cnlName

    this._setColor(this._getColorByTrend(trendInd))
    this._context.fillRect(rectX, rectY, rectSize, rectSize)
    this._setColor(layout.LBL_COLOR)
    this._context.strokeRect(rectX, rectY, rectSize, rectSize)
    this._context.fillText(legendText, lblX, lblY)

    lblY += layout.LINE_HEIGHT
    rectY += layout.LINE_HEIGHT
  }
}

// Draw all the trends
scada.chart.Chart.prototype._drawTrends = function (opt_startPtInd) {
  const trendCnt = this.chartData.trends.length
  for (let trendInd = 0; trendInd < trendCnt; trendInd++) {
    this._drawTrend(this.chartData.timePoints, this.chartData.trends[trendInd],
      this._getColorByTrend(trendInd), opt_startPtInd)
  }
}

// Draw the specified trend
scada.chart.Chart.prototype._drawTrend = function (timePoints, trend, color, opt_startPtInd) {
  const trendPoints = trend.trendPoints
  const chartGap = this.displaySettings.chartGap
  const VAL_IND = scada.chart.TrendPointIndexes.VAL_IND

  this._setColor(color)

  let prevX = NaN
  let prevPtX = NaN
  let prevPtY = NaN
  const startPtInd = opt_startPtInd || 0
  const ptCnt = timePoints.length

  for (let ptInd = startPtInd; ptInd < ptCnt; ptInd++) {
    const pt = trendPoints[ptInd]
    const y = pt[VAL_IND]

    if (!isNaN(y)) {
      const x = timePoints[ptInd]
      const ptX = this._trendXToChartX(x)
      const ptY = this._trendYToChartY(y)

      if (isNaN(prevX)) {
      } else if (x - prevX > chartGap) {
        this._drawPixel(prevPtX, prevPtY, true)
        this._drawPixel(ptX, ptY, true)
      } else if (prevPtX != ptX || prevPtY != ptY) {
        this._drawLine(prevPtX, prevPtY, ptX, ptY, true)
      }

      prevX = x
      prevPtX = ptX
      prevPtY = ptY
    }
  }

  if (!isNaN(prevPtX)) { this._drawPixel(prevPtX, prevPtY, true) }
}

// Create a time mark if it doesn't exist
scada.chart.Chart.prototype._initTimeMark = function () {
  if (this._timeMark) {
    this._timeMark.addClass('hidden')
  } else {
    this._timeMark = $("<div class='chart-time-mark hidden'></div>")
    this._canvasJqObj.after(this._timeMark)
  }
}

// Create a trend hint if it doesn't exist
scada.chart.Chart.prototype._initTrendHint = function () {
  if (this._trendHint) {
    this._trendHint.addClass('hidden')
  } else {
    const trendCnt = this.chartData.trends.length
    if (trendCnt > 0) {
      this._trendHint = $("<div class='chart-trend-hint hidden'><div class='time'></div><table></table></div>")
      const table = this._trendHint.children('table')

      for (let trendInd = 0; trendInd < trendCnt; trendInd++) {
        const trend = this.chartData.trends[trendInd]
        const row = $("<tr><td class='color'><span></span></td><td class='text'></td>" +
                    "<td class='colon'>:</td><td class='val'></td></tr>")
        row.find('td.color span').css('background-color', this._getColorByTrend(trendInd))
        row.children('td.text').text('[' + trend.cnlNum + '] ' + trend.cnlName)
        table.append(row)
      }

      this._canvasJqObj.after(this._trendHint)
    } else {
      this._trendHint = $()
    }
  }
}

// Show hint with the values nearest to the pointer
scada.chart.Chart.prototype._showHint = function (pageX, pageY, opt_touch) {
  const layout = this._chartLayout
  let hideHint = true
  layout.updateAbsCoordinates(this._canvasJqObj)

  if (this._hintEnabled && layout.pointInPlotArea(pageX, pageY)) {
    const ptInd = this._getPointIndex(pageX)

    if (ptInd >= 0) {
      const x = this.chartData.timePoints[ptInd]
      const ptPageX = this._trendXToPageX(x)

      if (layout.absPlotAreaLeft <= ptPageX && ptPageX <= layout.absPlotAreaRight) {
        hideHint = false

        // set position and show the time mark
        this._timeMark
          .removeClass('hidden')
          .css({
            left: ptPageX - layout.absCanvasLeft,
            top: layout.canvasTopBorder + layout.plotAreaTop,
            height: layout.plotAreaHeight
          })

        // set text, position and show the trend hint
        this._trendHint.find('div.time').text(this._showDates ? this._dateTimeToStr(x) : this._timeToStr(x))
        const trendCnt = this.chartData.trends.length
        const hintValCells = this._trendHint.find('td.val')

        for (let trendInd = 0; trendInd < trendCnt; trendInd++) {
          const trend = this.chartData.trends[trendInd]
          const trendPoint = trend.trendPoints[ptInd]
          hintValCells.eq(trendInd)
            .text(trendPoint[scada.chart.TrendPointIndexes.TEXT_WITH_UNIT_IND])
            .css('color', trendPoint[scada.chart.TrendPointIndexes.COLOR_IND])
        }

        // allow measuring the hint size
        this._trendHint
          .css({
            left: 0,
            top: 0,
            visibility: 'hidden'
          })
          .removeClass('hidden')

        const hintWidth = this._trendHint.outerWidth()
        const hintHeight = this._trendHint.outerHeight()
        const winScrollLeft = $(window).scrollLeft()
        const winRight = winScrollLeft + $(window).width()
        const chartRight = winScrollLeft + layout.absCanvasLeft + layout.canvasLeftBorder + layout.width
        const maxRight = Math.min(winRight, chartRight)
        const absHintLeft = pageX + hintWidth < maxRight ? pageX : Math.max(pageX - hintWidth, 0)

        this._trendHint.css({
          left: absHintLeft - layout.absCanvasLeft,
          top: pageY - layout.absCanvasTop - hintHeight -
                        (opt_touch ? layout.HINT_OFFSET /* above a finger */ : 0),
          visibility: ''
        })
      }
    }
  }

  if (hideHint) {
    this._timeMark.addClass('hidden')
    this._trendHint.addClass('hidden')
  }
}

// Draw the chart
scada.chart.Chart.prototype.draw = function () {
  if (this._canvasOK && this.displaySettings && this.timeRange && this.chartData) {
    // initialize displayed range on the first using
    this._initRange()

    // prepare canvas
    const layout = this._chartLayout
    this._canvas.width = this._canvasJqObj.width()
    this._canvas.height = this._canvasJqObj.height()
    this._context = this._canvas.getContext('2d')
    this._context.font = layout.LBL_FONT
    this._initTimeMark()
    this._initTrendHint()

    // calculate layout
    const trendCnt = this.chartData.trends.length
    layout.calculate(this._canvasJqObj, this._context,
      this._minX, this._maxX, this._minY, this._maxY, trendCnt, this._showDates)

    this._alignToGridX()
    this._coefX = (layout.plotAreaWidth - 1) / (this._maxX - this._minX)
    this._coefY = (layout.plotAreaHeight - 1) / (this._maxY - this._minY)

    // draw chart
    this._clearRect(0, 0, layout.width, layout.height)
    this._drawFrame()
    this._drawGridX()
    this._drawGridY()
    this._drawYAxisTitle()
    this._drawLegend()
    this._drawTrends()
  }
}

// Resume drawing of the chart
scada.chart.Chart.prototype.resume = function (pointInd) {
  if (pointInd < this.chartData.timePoints.length) {
    if (this._yRangeIsOutdated(pointInd)) {
      this._calcYRange()
      this.draw()
    } else {
      this._drawTrends(pointInd ? pointInd - 1 : 0)
    }
  }
}

// Set displayed time range
scada.chart.Chart.prototype.setRange = function (startX, endX) {
  // swap the range if needed
  if (startX > endX) {
    const xbuf = startX
    startX = endX
    endX = xbuf
  }

  // correct the range
  startX = Math.max(startX, this.timeRange.startTime)
  endX = Math.min(endX, this.timeRange.endTime)

  // apply the new range
  if (startX != endX) {
    this._minX = startX
    this._maxX = endX
    this._zoomMode = this._minX > this.timeRange.startTime || this._maxX < this.timeRange.endTime
    this._calcYRange()
    this.draw()
  }
}

// Reset displayed time range according to the chart time range
scada.chart.Chart.prototype.resetRange = function () {
  this._initRange(true)
  this.draw()
}

// Convert x-coordinate that means time into a date string
scada.chart.Chart.prototype.dateToStr = function (t) {
  const date = this._trendXToDate(t)
  if (scada.utils.iOS()) {
    date.setUTCMinutes(date.getUTCMinutes() + date.getTimezoneOffset())
  }
  return date.toLocaleDateString(this.displaySettings.locale, this._DATE_OPTIONS)
}

// Convert x-coordinate that means time into a time string ignoring culture with high performance
scada.chart.Chart.prototype.fastTimeToStr = function (t, opt_showSeconds) {
  const time = new Date(Math.round(t * scada.chart.const.MS_PER_DAY))
  return this._simpleTimeToStr(time, opt_showSeconds)
}

// Bind events to allow hints
scada.chart.Chart.prototype.bindHintEvents = function () {
  if (this._canvasJqObj.length) {
    const thisObj = this

    $(this._canvasJqObj.parent())
      .off('.scada.chart.hint')
      .on('mousemove.scada.chart.hint touchstart.scada.chart.hint touchmove.scada.chart.hint', function (event) {
        let touch = false
        let stopEvent = false

        if (event.type == 'touchstart') {
          event = event.originalEvent.touches[0]
          touch = true
        } else if (event.type == 'touchmove') {
          $(this).off('mousemove')
          event = event.originalEvent.touches[0]
          touch = true
          stopEvent = true
        }

        thisObj._showHint(event.pageX, event.pageY, touch)
        return !stopEvent
      })
  }
}
