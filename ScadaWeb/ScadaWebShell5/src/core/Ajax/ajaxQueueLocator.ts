import { AjaxQueue } from "./ajaxQueue";

export class WindowEx extends Window {
    public ajaxQueue: AjaxQueue;
}

export class AjaxQueueLocator {
    public static getAjaxQueue(): AjaxQueue {
        var wnd = window as WindowEx;
        while (wnd) {
            if (wnd.ajaxQueue) {
                return wnd.ajaxQueue;
            }
            window = wnd === window.top ? null : window.parent;
        }
        return null;
    }
}