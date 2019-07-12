export enum EventTypes {
    // Page layout should be updated
    UPDATE_LAYOUT = "scada:updateLayout",

    // View title has been changed.
    // Event parameter: title
    VIEW_TITLE_CHANGED = "scada:viewTitleChanged",

    // Current view date has been changed
    // Event parameter: date
    VIEW_DATE_CHANGED = "scada:viewDateChanged",

    // Modal dialog button is clicked
    // Event parameter: dialog result
    MODAL_BTN_CLICK = "scada:modalBtnClick"
};