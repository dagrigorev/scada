/*
 * Popup dialogs manipulation
 *
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 *
 * Requires:
 * - jquery
 * - utils.js
 *
 * Requires for modal dialogs:
 * - bootstrap
 * - eventtypes.js
 * - scada.modalButtonCaptions object
 */

// Rapid SCADA namespace
var scada = scada || {}

/** ******** Modal Dialog Buttons **********/

// Modal dialog buttons enumeration
scada.ModalButtons = {
  OK: 'ok',
  YES: 'yes',
  NO: 'no',
  EXEC: 'execute',
  CANCEL: 'cancel',
  CLOSE: 'close'
}

/** ******** Modal Dialog Sizes **********/

// Modal dialog sizes enumeration
scada.ModalSizes = {
  NORMAL: 0,
  SMALL: 1,
  LARGE: 2
}

/** ******** Modal Dialog Options **********/

// Modal dialog options class
scada.ModalOptions = function (buttons, opt_size) {
  this.buttons = buttons
  this.size = opt_size || scada.ModalSizes.NORMAL
}

/** ******** Popup **********/

// Popup dialogs manipulation type
scada.Popup = function () {
  // Window that holds popups
  this._holderWindow = window
}

// Close the dropdown popup and execute a callback with a cancel result
scada.Popup.prototype._cancelDropdown = function (popupElem) {
  const callback = popupElem.data('popup-callback')
  popupElem.remove()

  if (callback) {
    callback(false)
  }
}

// Get coodinates of the specified element relative to the holder window
scada.Popup.prototype._getOffset = function (elem) {
  // validate the element
  const defaultOffset = { left: 0, top: 0 }
  if (!(elem && elem.length)) {
    return defaultOffset
  }

  // get coodinates within a window that contains the element
  let wnd = elem[0].ownerDocument.defaultView
  let offset = elem.offset()
  let left = offset.left + $(wnd).scrollLeft()
  let top = offset.top + $(wnd).scrollTop()

  // add coordinates of the parent frames
  do {
    const parentWnd = wnd.parent
    if (wnd != parentWnd) {
      if (parentWnd.$) {
        const frame = parentWnd.$(wnd.frameElement)
        if (frame.length > 0) {
          offset = frame.offset()
          left += offset.left + $(parentWnd).scrollLeft()
          top += offset.top + $(parentWnd).scrollTop()
        }
      } else {
        console.warn('Unable to get offset, because jQuery is not found')
        return defaultOffset
      }
      wnd = parentWnd
    }
  } while (wnd != this._holderWindow && wnd != wnd.parent)

  return { left: left, top: top }
}

// Get caption for the specified modal dialog button
scada.Popup.prototype._getModalButtonCaption = function (btn) {
  let btnCaption = scada.modalButtonCaptions ? scada.modalButtonCaptions[btn] : null
  if (!btnCaption) {
    btnCaption = btn
  }
  return btnCaption
}

// Get html markup of a modal dialog footer buttons
scada.Popup.prototype._genModalButtonsHtml = function (buttons) {
  let html = ''

  for (const btn of buttons) {
    const btnCaption = this._getModalButtonCaption(btn)
    const subclass = btn == scada.ModalButtons.OK || btn == scada.ModalButtons.YES ? 'btn-primary'
      : (btn == scada.ModalButtons.EXEC ? 'btn-danger' : 'btn-default')
    const dismiss = btn == scada.ModalButtons.CANCEL || btn == scada.ModalButtons.CLOSE
      ? " data-dismiss='modal'" : ''

    html += "<button type='button' class='btn " + subclass +
            "' data-result='" + btn + "'" + dismiss + '>' + btnCaption + '</button>'
  }

  return html
}

// Find modal button by result
scada.Popup.prototype._findModalButton = function (modalWnd, btn) {
  const frame = $(modalWnd.frameElement)
  const modalElem = frame.closest('.modal')
  return modalElem.find(".modal-footer button[data-result='" + btn + "']")
}

// Show popup with the specified url as a dropdown menu below the anchorElem.
// opt_callback is a function (dialogResult, extraParams)
scada.Popup.prototype.showDropdown = function (url, anchorElem, opt_callback) {
  const thisObj = this
  const popupElem = $("<div class='popup-dropdown'><div class='popup-overlay'></div>" +
        "<div class='popup-wrapper'><iframe class='popup-frame'></iframe></div></div>")

  if (opt_callback) {
    popupElem.data('popup-callback', opt_callback)
  }

  $('body').append(popupElem)

  const overlay = popupElem.find('.popup-overlay')
  const wrapper = popupElem.find('.popup-wrapper')
  const frame = popupElem.find('.popup-frame')

  // setup overlay
  overlay
    .css('z-index', scada.utils.FRONT_ZINDEX)
    .click(function () {
      thisObj._cancelDropdown(popupElem)
    })

  // setup wrapper
  wrapper.css({
    'z-index': scada.utils.FRONT_ZINDEX + 1, // above the overlay
    opacity: 0.0 // hide the popup while it's loading
  })

  // remove the popup on press Escape key in the parent window
  const removePopupOnEscapeFunc = function (event) {
    if (event.which == 27 /* Escape */) {
      thisObj._cancelDropdown(popupElem)
    }
  }

  $(document)
    .off('keydown.scada.dropdown', removePopupOnEscapeFunc)
    .on('keydown.scada.dropdown', removePopupOnEscapeFunc)

  // load the frame
  frame
    .on('load', function () {
      // remove the popup on press Escape key in the frame
      const frameWnd = frame[0].contentWindow
      if (frameWnd.$) {
        const jqFrameDoc = frameWnd.$(frameWnd.document)
        jqFrameDoc.ready(function () {
          jqFrameDoc
            .off('keydown.scada.dropdown', removePopupOnEscapeFunc)
            .on('keydown.scada.dropdown', removePopupOnEscapeFunc)
        })
      }
    })
    .one('load', function () {
      // set the popup position
      const frameBody = frame.contents().find('body')
      const width = frameBody.outerWidth(true)
      const height = frameBody.outerHeight(true)
      let left = 0
      let top = 0

      if (anchorElem.length > 0) {
        const offset = thisObj._getOffset(anchorElem)
        left = offset.left
        top = offset.top + anchorElem.outerHeight()
        const borderWidthX2 = parseInt(wrapper.css('border-width'), 10) * 2

        if (left + width + borderWidthX2 > $(document).width()) { left = Math.max($(document).width() - width - borderWidthX2, 0) }

        if (top + height + borderWidthX2 > $(document).height()) { top = Math.max($(document).height() - height - borderWidthX2, 0) }
      } else {
        left = Math.max(($(window).width() - width) / 2, 0)
        top = Math.max(($(window).height() - height) / 2, 0)
      }

      wrapper.css({
        left: left,
        top: top
      })

      // set the popup size and display the popup
      frame
        .css({
          width: width,
          height: height
        })
        .focus()

      wrapper.css({
        width: width,
        height: height,
        opacity: 1.0
      })
    })
    .attr('src', url)
}

// Close the dropdown popup and execute a callback with the specified result
scada.Popup.prototype.closeDropdown = function (popupWnd, dialogResult, extraParams) {
  const frame = $(popupWnd.frameElement)
  const popupElem = frame.closest('.popup-dropdown')
  const callback = popupElem.data('popup-callback')
  popupElem.remove()

  if (callback) {
    callback(dialogResult, extraParams)
  }
}

// Show modal dialog with the specified url.
// opt_callback is a function (dialogResult, extraParams),
// requires Bootstrap
scada.Popup.prototype.showModal = function (url, opt_options, opt_callback) {
  // create temporary overlay to prevent user activity
  const tempOverlay = $("<div class='popup-overlay'></div>")
  $('body').append(tempOverlay)

  // create the modal
  const buttons = opt_options ? opt_options.buttons : null
  const footerHtml = buttons && buttons.length
    ? "<div class='modal-footer'>" + this._genModalButtonsHtml(buttons) + '</div>' : ''

  const size = opt_options ? opt_options.size : scada.ModalSizes.NORMAL
  let sizeClass = ''
  if (size == scada.ModalSizes.SMALL) {
    sizeClass = ' modal-sm'
  } else if (size == scada.ModalSizes.LARGE) {
    sizeClass = ' modal-lg'
  }

  const modalElem = $(
    "<div class='modal fade' tabindex='-1'>" +
        "<div class='modal-dialog" + sizeClass + "'>" +
        "<div class='modal-content'>" +
        "<div class='modal-header'>" +
        "<button type='button' class='close' data-dismiss='modal'><span>&times;</span></button>" +
        "<h4 class='modal-title'></h4></div>" +
        "<div class='modal-body'></div>" +
        footerHtml +
        '</div></div></div>')

  if (opt_callback) {
    modalElem
      .data('modal-callback', opt_callback)
      .data('dialog-result', false)
  }

  // create the frame
  const modalFrame = $("<iframe class='modal-frame'></iframe>")
  modalFrame.css({
    position: 'fixed',
    opacity: 0.0 // hide the frame while it's loading
  })
  $('body').append(modalFrame)

  // create a function that hides the modal on press Escape key
  const hideModalOnEscapeFunc = function (event) {
    if (event.which == 27 /* Escape */) {
      modalElem.modal('hide')
    }
  }

  // load the frame
  modalFrame
    .on('load', function () {
      // remove the modal on press Escape key in the frame
      const frameWnd = modalFrame[0].contentWindow
      if (frameWnd.$) {
        const jqFrameDoc = frameWnd.$(frameWnd.document)
        jqFrameDoc.ready(function () {
          jqFrameDoc
            .off('keydown.scada.modal', hideModalOnEscapeFunc)
            .on('keydown.scada.modal', hideModalOnEscapeFunc)
        })
      }
    })
    .one('load', function () {
      // get the frame size
      const frameBody = modalFrame.contents().find('body')
      const frameWidth = frameBody.outerWidth(true)
      const frameHeight = frameBody.outerHeight(true)

      // tune the modal
      const modalBody = modalElem.find('.modal-body')
      const modalPaddings = parseInt(modalBody.css('padding-left')) + parseInt(modalBody.css('padding-right'))
      modalElem.find('.modal-content').css('min-width', frameWidth + modalPaddings)
      modalElem.find('.modal-title').text(modalFrame[0].contentWindow.document.title)

      // move the frame into the modal
      modalFrame.detach()
      modalBody.append(modalFrame)
      $('body').append(modalElem)

      // set the frame style
      modalFrame.css({
        width: '100%',
        height: frameHeight,
        position: '',
        opacity: 1.0
      })

      // raise event on modal button click
      modalElem.find('.modal-footer button').click(function () {
        const result = $(this).data('result')
        const frameWnd = modalFrame[0].contentWindow
        const frameJq = frameWnd.$
        if (result && frameJq) {
          frameJq(frameWnd).trigger(scada.EventTypes.MODAL_BTN_CLICK, result)
        }
      })

      // display the modal
      modalElem
        .on('shown.bs.modal', function () {
          tempOverlay.remove()
          modalFrame.focus()
        })
        .on('hidden.bs.modal', function () {
          const callback = $(this).data('modal-callback')
          if (callback) {
            callback($(this).data('dialog-result'), $(this).data('extra-params'))
          }

          $(this).remove()
        })
        .modal('show')
    })
    .attr('src', url)
}

// Close the modal dialog
scada.Popup.prototype.closeModal = function (modalWnd, dialogResult, extraParams) {
  this.setModalResult(modalWnd, dialogResult, extraParams).modal('hide')
}

// Update the modal dialog height according to a frame height
scada.Popup.prototype.updateModalHeight = function (modalWnd) {
  const frame = $(modalWnd.frameElement)
  const frameBody = frame.contents().find('body')
  const modalElem = frame.closest('.modal')

  const iosScrollFix = scada.utils.iOS()
  if (iosScrollFix) {
    modalElem.css('overflow-y', 'hidden')
  }

  frame.css('height', frameBody.outerHeight(true))

  if (iosScrollFix) {
    modalElem.css('overflow-y', '')
  }

  modalElem.modal('handleUpdate')
}

// Set dialog result for the whole modal dialog
scada.Popup.prototype.setModalResult = function (modalWnd, dialogResult, extraParams) {
  const frame = $(modalWnd.frameElement)
  const modalElem = frame.closest('.modal')
  modalElem
    .data('dialog-result', dialogResult)
    .data('extra-params', extraParams)
  return modalElem
}

// Show or hide the button of the modal dialog
scada.Popup.prototype.setButtonVisible = function (modalWnd, btn, val) {
  this._findModalButton(modalWnd, btn).css('display', val ? '' : 'none')
}

// Enable or disable the button of the modal dialog
scada.Popup.prototype.setButtonEnabled = function (modalWnd, btn, val) {
  const btnElem = this._findModalButton(modalWnd, btn)
  if (val) {
    btnElem.removeAttr('disabled')
  } else {
    btnElem.attr('disabled', 'disabled')
  }
}

/** ******** Popup Locator **********/

// Popup locator object
scada.popupLocator = {
  // Find and return an existing popup object
  getPopup: function () {
    let wnd = window
    while (wnd) {
      if (wnd.popup) {
        return wnd.popup
      }
      wnd = wnd == window.top ? null : window.parent
    }
    return null
  }
}
