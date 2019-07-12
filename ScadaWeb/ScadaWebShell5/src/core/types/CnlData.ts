export class CnlData {
    public Val: number;
    public Stat: number;

    constructor() {
        this.Val = 0.0;
        this.Stat = 0;
    }
}

export class CnlDataEx extends CnlData{
    public CnlNum: number;
    public Text: string;
    public TextWithUnit: string;
    public Color: string;

    constructor() {
        super();

        this.CnlNum = 0;
        this.Color = "";
        this.Text = "";
        this.TextWithUnit = "";
    }
}

export class HourCnlData {
    public Hour: number;
    public Modified: boolean;
    public CnlDataExtArr: CnlDataEx[];

    constructor() {
        this.CnlDataExtArr = [];
        this.Hour = NaN;
        this.Modified = false;
    }
}