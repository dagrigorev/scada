import { AjaxQueue } from "./ajaxQueue";
import { ViewHub } from "../api/ViewHub";

export class WindowEx extends Window {
    public ajaxQueue: AjaxQueue;
    public env: any;
    public scada: any;
    public $: JQueryStatic;
    public viewHub: ViewHub;
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