export enum ExportFormats {
    PDF = "pdf",
    PNG = "png",
    EXCEL = "xml"
}

const EXPORT_BTN_LOCK: number = 3000;

export class Export {
    public _addLeadingZero(value: number): string {
        return value < 10 ? "0" + value.toString() : value.toString(); 
    }

    public buildFileName(prefix: string, extension: string): string {
        var now = new Date();
        return prefix + "_" + now.getFullYear() + "-" + this._addLeadingZero((now.getMonth() + 1)) + "-" +
            this._addLeadingZero(now.getDate()) + "_" + this._addLeadingZero(now.getHours()) + "-" +
            this._addLeadingZero(now.getMinutes()) + "-" + this._addLeadingZero(now.getSeconds()) + "." +
            extension;
    }

    public lockExportButton(jqExportBtn: JQuery): void {
        jqExportBtn.prop("disabled", true);
        setTimeout(function () { jqExportBtn.prop("disabled", false); }, EXPORT_BTN_LOCK);
    }
}