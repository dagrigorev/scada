import { WindowEx } from "../Ajax/index";
import { EventTypes } from "./EventTypes";
import { getViewUrl } from "../../utils/utils";

export class ViewHub {
    public curViewID: number;
    public curViewDateMs: number;
    public mainWindow: WindowEx;
    public viewWindow: WindowEx;
    public dataWindow: WindowEx;
    public dialogs: any; // FIXME: Change to 'Dialogs'

    constructor(mainWindow: WindowEx) {
        this.curViewDateMs = 0;
        this.curViewID = 0;
        this.dataWindow = null;
        this.viewWindow = null;
        this.mainWindow = mainWindow;
    }

    private _getEnvObject(): any {
        return this.mainWindow && this.mainWindow.scada ? this.mainWindow.env : null;
    }

    public addView(wnd: WindowEx): void {
        this.viewWindow = wnd;
    }

    public addDataWindow(wnd: WindowEx): void {
        this.dataWindow = wnd;
    }

    public removeDataWindow(): void {
        this.dataWindow = null;
    }

    public notify(eventType: EventTypes, senderWnd: WindowEx, opt_extraParams: Date): void {
        // preprocess events
        if (eventType == EventTypes.VIEW_DATE_CHANGED) {
            this.curViewDateMs = opt_extraParams.getTime();
        }

        // pass the notification to the main window
        if (this.mainWindow && this.mainWindow != senderWnd) {
            var jq = this.mainWindow.$;
            if (jq) {
                jq(this.mainWindow).trigger(eventType, [senderWnd, opt_extraParams]);
            }
        }

        // pass the notification to the view window
        if (this.viewWindow && this.viewWindow != senderWnd) {
            var jq = this.viewWindow.$;
            if (jq) {
                jq(this.viewWindow).trigger(eventType, [senderWnd, opt_extraParams]);
            }
        }

        // pass the notification to the data window
        if (this.dataWindow && this.dataWindow != senderWnd) {
            var jq = this.dataWindow.$;
            if (jq) {
                jq(this.dataWindow).trigger(eventType, [senderWnd, opt_extraParams]);
            }
        }
    }

    public getFullViewUrl(viewID: number, opt_isPopup: boolean): string {
        var env = this._getEnvObject();
        return (env ? env.rootPath : "") + getViewUrl(viewID, opt_isPopup);
    }
}

export class ViewHubLocator {
    public static getViewHub(): ViewHub {
        var wnd = window as WindowEx;
        while (wnd) {
            if (wnd.viewHub) {
                return wnd.viewHub;
            }
            window = wnd == window.top ? null : window.parent;
        }
        return null;
    }
}