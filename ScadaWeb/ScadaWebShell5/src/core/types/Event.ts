export default class Event {
    public Num: number;
    public Time: string;
    public Obj: string;
    public KP: string;
    public Cnl: string;
    public Text: string;
    public Ack: string;
    public Color: string;
    public Sound: boolean;

    constructor() {
        this.Num = 0;
        this.Time = "";
        this.Obj = "";
        this.KP = "";
        this.Cnl = "";
        this.Text = "";
        this.Ack = "";
        this.Color = "";
        this.Sound = false;
    }
}