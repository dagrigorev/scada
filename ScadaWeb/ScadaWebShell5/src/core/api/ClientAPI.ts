import { CnlData, CnlDataEx } from "../types/index";
import { AjaxQueue } from "../Ajax/index";
import { logSuccessfulRequest, logServiceError, logProcessingError, logFailedRequest } from "../../utils/utils";
import { AjaxData, ParsedAjaxData } from "../ajax/AjaxData";

export default class ClientAPI {
    private _EMPTY_CNL_DATA: Readonly<CnlData>;
    private _EMPTY_CNL_DATA_EXT: Readonly<CnlDataEx>;
    public rootPath: string;
    private ajaxQueue: AjaxQueue;

    constructor() {
        this.ajaxQueue = null;
        this.rootPath = "";
        this._EMPTY_CNL_DATA = new CnlData();
        this._EMPTY_CNL_DATA_EXT = new CnlDataEx();
    }

    public _request(operation: string, queryString: string, callback: (result: boolean, errorCode: number, age: number) => void, errorResult): void {
        var ajaxObj = this.ajaxQueue ? this.ajaxQueue : $;

        /*{
            url: (this.ajaxQueue ? this.ajaxQueue.rootPath : this.rootPath) + operation + queryString,
                method: "GET",
                    dataType: "json",
                        cache: false
        }*/

        ajaxObj.ajax(
            this.ajaxQueue ? this.ajaxQueue.rootPath : this.rootPath,
            {
                method: "GET",
                dataType: "json",
                cache: false
            }
        ).done((data: AjaxData, textStatus: string, jqXHR: JQueryXHR) => {
            try {
                var parsedData = $.parseJSON(data.d) as ParsedAjaxData;
                    if (parsedData.Success) {
                        logSuccessfulRequest(operation, data);
                        if (typeof parsedData.DataAge === undefined) {
                            callback(true, parsedData.Data, 0);
                        } else {
                            callback(true, parsedData.Data, parsedData.DataAge);
                        }
                    } else {
                        logServiceError(operation, parsedData.ErrorMessage);
                        callback(false, errorResult, 0);
                    }
                }
                catch (ex) {
                    logProcessingError(operation, ex.message);
                    if (typeof callback === "function") {
                        callback(false, errorResult, 0);
                    }
                }
            })
            .fail(function (jqXHR, textStatus, errorThrown) {
                logFailedRequest(operation, jqXHR);
                if (typeof callback === "function") {
                    callback(false, errorResult, 0);
                }
            });
    }
}