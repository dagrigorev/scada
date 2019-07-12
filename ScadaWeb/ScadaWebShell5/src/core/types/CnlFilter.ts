export default class CnlFilter {
    public cnlNums: number[];
    public viewIDs: number[];
    public viewID: number;

    constructor() {
        this.cnlNums = [];
        this.viewIDs = [];
        this.viewID = 0;
    }

    public toQueryString(): string {
        // TODO: Implement this
        /*return "cnlNums=" + scada.utils.arrayToQueryParam(this.cnlNums) +
            "&viewIDs=" + scada.utils.arrayToQueryParam(this.viewIDs) +
            "&viewID=" + (this.viewID ? this.viewID : 0);*/
        return "";
    }
}