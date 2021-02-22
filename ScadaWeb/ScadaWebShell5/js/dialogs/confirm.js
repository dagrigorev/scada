$(document).ready(function () {
  // close the modal on button click
  $(window).on(scada.EventTypes.MODAL_BTN_CLICK, function (event, result) {
    const popup = scada.popupLocator.getPopup()
    if (popup) {
      popup.closeModal(window, result == scada.ModalButtons.YES)
    }
  })
})
