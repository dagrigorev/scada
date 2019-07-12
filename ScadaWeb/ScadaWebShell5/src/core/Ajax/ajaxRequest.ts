import Settings from './Settings';
import { AjaxQueue } from './index';

export class AjaxRequest {
    public settings: Settings;
    public doneCallback: (data, textStatus, jqXHR) => void;
    public failCallback: (data, textStatus, jqXHR) => void;
    public alwaysCallback: (data, textStatus, jqXHR) => void;

    constructor(settings: Settings) {
        this.settings = settings;
        this.alwaysCallback = null;
        this.doneCallback = null;
        this.failCallback = null;
    }

    /**
     * Sets done callback
     * @param doneCallback
     */
    public done(doneCallback: () => void): AjaxRequest {
        this.doneCallback = doneCallback;
        return this;
    }

    /**
     * Sets fail callback
     * @param failCallback
     */
    public fail(failCallback: () => void): AjaxRequest {
        this.failCallback = failCallback;
        return this;
    }

    /**
     * Sets always callback
     * @param alwaysCallback
     */
    public always(alwaysCallback: () => void): AjaxRequest {
        this.alwaysCallback = alwaysCallback;
        return this;
    }

    /**
     * Appends new request to queue
     * @param ajaxQueue
     */
    public appendTo(ajaxQueue: AjaxQueue): AjaxRequest {
        ajaxQueue.append(this);
        return this;
    }
}