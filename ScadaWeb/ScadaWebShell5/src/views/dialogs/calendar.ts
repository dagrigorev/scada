export function selectDate(year: number, month: number, day: number, dateStr: string): void {
    var popup = PopupLocator.getPopup();
    if (popup) {
        popup.closeDropdown(window, true, { date: new Date(year, month - 1, day), dateStr: dateStr });
    }
}

$(document).ready(() => {
    $("#frmCalendar *").css({
        "background-color": "",
        "color": ""
    });
});