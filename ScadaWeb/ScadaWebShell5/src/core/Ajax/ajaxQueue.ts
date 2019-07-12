import { AjaxRequest } from "./ajaxRequest";
import * as $ from 'jquery';
import Settings from "./Settings";

/*
 * Queue for sending Ajax requests one after another.
 * Allows to avoid Mono WCF bug and improves load balancing.
 * Based on ajaxqueue.js written by Mikhail Shiryaev.
 *
 * Author   : Dmitry Grigorev
 * Created  : 2019
 * Modified : 2019
 *
 * Requires:
 * - jquery
 */

/**
 * Abnormal size of the queue. Need to warn a user
 **/
export const WARN_QUEUE_SIZE: number = 100;

/**
 * AjaxQueue type implementation
 * */
export class AjaxQueue {
    public rootPath: string;
    public requests: AjaxRequest[];
    public timeoutID: number;

    /**
     * Appends new request to queue then execute it
     * @param ajaxRequest
     */
    public append(ajaxRequest: AjaxRequest): void {
        if (ajaxRequest) {
            this.requests.push(ajaxRequest);

            if (this.requests.length >= WARN_QUEUE_SIZE) {
                console.warn("Ajax queue size is " + this.requests.length);
            }

            this._run();
        }
    }

    /**
     * Perform the first request from the queue and initiate sending of the next one
     **/
    private _request(): void {
        if (this.requests.length > 0) {
            var thisObj = this;
            var ajaxRequest = this.requests.shift();

            $.ajax(ajaxRequest.settings)
                .done(function (data, textStatus, jqXHR) {
                    if (typeof ajaxRequest.doneCallback === "function") {
                        ajaxRequest.doneCallback(data, textStatus, jqXHR);
                    }
                })
                .fail(function (jqXHR, textStatus, errorThrown) {
                    if (typeof ajaxRequest.failCallback === "function") {
                        ajaxRequest.failCallback(jqXHR, textStatus, errorThrown);
                    }
                })
                .always(function (data_jqXHR, textStatus, jqXHR_errorThrown) {
                    if (typeof ajaxRequest.alwaysCallback === "function") {
                        ajaxRequest.alwaysCallback(data_jqXHR, textStatus, jqXHR_errorThrown);
                    }

                    if (thisObj.requests.length > 0) {
                        thisObj.timeoutID = setTimeout(thisObj._request.bind(thisObj), 0);
                    } else {
                        thisObj.timeoutID = 0;
                    }
                });
        }
    }

    /**
     * Start sending process if it is not running
     **/
    private _run(): void {
        if (this.timeoutID <= 0) {
            this.timeoutID = setTimeout(this._request.bind(this), 0);
        }
    }

    /**
     * Create new Ajax request and append it to the queue
     * @param settings
     */
    public ajax(settings: Settings): AjaxRequest {
        var ajaxRequest = new AjaxRequest(settings);
        this.append(ajaxRequest);
        return ajaxRequest;
    }
}