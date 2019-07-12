export class HourPeriod {
    public date: Date;
    public startHour: number;
    public endHour: number;

    constructor() {
        // FIXME: Check this date assignment
        this.date = new Date(0);
        this.endHour = 0;
        this.startHour = 0;
    }

    public toQueryString(): string {
        return `year=${this.date.getFullYear()}
            &month=${(this.date.getMonth() + 1)}
            &day=${this.date.getDate()}
            &startHour=${this.startHour}
            &endHour=${this.endHour}`;
    }
}

export enum HourDataModes {
    // Select data for integer hours even if a snapshot doesn't exist
    INTEGER_HOURS = 0,
    // Select existing hourly snapshots
    EXISTING = 1
}