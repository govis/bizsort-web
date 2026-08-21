import { SessionException, SessionExceptionType } from '../exception.js';
import { ApiConfig as ServiceSettings } from '../settings.js';

export class WebAppImage {
    static getImageRef(app: string, alias: string, size: WebAppImage.Size) {
        //https://github.com/PolymerElements/iron-image/pull/117
        return ServiceSettings.baseUrl + '/image/get_App?app=' + app + '&alias=' + alias + '&size=' + size;
    }

    constructor(public app: string, public alias: string, public size: WebAppImage.Size, object?: WebAppImage) {
        if (object)
            Object.assign(this, object);
    }

    type!: string;
    token?: string;

    get imageRef(): string {
        if (this.alias && this.size)
            return WebAppImage.getImageRef(this.app, this.alias, this.size);
        return '/images/bizsort-logo.svg';
    }
}

export namespace WebAppImage {
    export enum Size {
        Icon_512 = 1,
        Icon_192 = 2
    }
}

export class OfferingStats {
    Total: number;
    TotalQuota: number;
    Active: number;
    ActiveQuota: number;
    Pending: number;
    PendingQuota: number;
    Inactive: number;
    constructor(totalQuota: number, activeQuota: number, pendingQuota: number) {
        this.Total = 0;
        this.TotalQuota = totalQuota;
        this.Active = 0;
        this.ActiveQuota = activeQuota;
        this.Pending = 0;
        this.PendingQuota = pendingQuota;
        this.Inactive = 0;
    }
    CanList() {
        return (this.Total < this.TotalQuota && this.Active < this.ActiveQuota && this.Pending < this.PendingQuota ? true : false);
    }
    Refresh(count: any) {
        this.Pending = count.Pending;
        this.Active = count.Active;
        this.Inactive = count.Inactive;
        this.Total = count.Total;
    }
    Test() {
        var quota = -1;
        var quotaType: string | undefined;
        if (this.Total >= this.TotalQuota) {
            quota = this.TotalQuota;
            quotaType = "Total";
        }
        else if (this.Active >= this.ActiveQuota) {
            quota = this.ActiveQuota;
            quotaType = "Active";
        }
        else if (this.Pending >= this.PendingQuota) {
            quota = this.PendingQuota;
            quotaType = "Pending";
        }
        if (quota >= 0) {
            throw new SessionException(SessionExceptionType.QuotaExceeded, (ex) => {
                ex["Quota"] = quota;
                if (quotaType)
                    ex["QuotaType"] = quotaType;
            });
        }
    }
}

