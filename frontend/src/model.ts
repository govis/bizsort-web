// @ts-nocheck
import type { EntityId, IdName, ILocation, ImageType, Geocoder, List, LocationRef, LocationType, ResolvedLocation, Semantic, ServiceProvider } from './model/foundation.js'
import { Image as ImageSettings, Service as ServiceSettings } from './settings.js'
import { SessionException, SessionExceptionType } from './exception.js'

export type { EntityId, IdName, ILocation, ImageType, Geocoder, List, LocationRef, LocationType, ResolvedLocation, Semantic, ServiceProvider }
export { ImageSettings, ServiceSettings, SessionException, SessionExceptionType }

export interface AccountId {
    accountType: AccountType;
    id: number;
}

export interface AccountName extends AccountId {
    name: string;
}

export interface Account extends AccountName {
    image?: Image;
}

export enum AccountType {
    Company = 1,
    Personal = 2
}

export namespace Address {
    export enum Requirement {
        None = 0,
        Country = 1,
        City = 2,
        PostalCode = 3,
        StreetAddress = 4
    }
}

export namespace ServiceType {
    export const Any = 2147483647;
    export enum ItemType {
        Design = 1,
        Make = 2,
        Custom_Build = 4,
        Install = 8,
        Operate = 16,
        Maintain_Repair = 32,
        Measure_Test = 64,
        Supply = 128,
        Dispose = 256
    }
}

export namespace Industry {
    export const Any = 2147483647;
}

export interface CompanyType extends DictionaryItem {
}

export interface CachedImage extends RawImage {
    type: ImageType,
    preview: RawImage
}

/*export namespace Currency {
    export enum ItemType {
        CAD = 1
    }
}

export interface Currency {
    Text: string
    CountryPriceFormat: string;
    PriceFormat: string;
}*/

export interface DictionaryItem {
    itemKey: number;
    itemText: string;
}

export enum DictionaryType {
    SecurityProfile = 1,
    ServiceType = 2,
    TransactionType = 3,
    Industry = 4,
    ProductType = 5,
    ProductPriceType = 6,
    ProductAttributeType = 7,
    Currency = 8
}

export interface IAddress {
    location: number;
    postalCode?: string;
    street?: number;
    streetNumber?: string;
    address1?: string;
    lat?: number;
    lng?: number;
    text?: string;
}

export interface IImage {
    imageRef: string;
    token?: string;
    width?: number;
    height?: number;
}

export class Image {
    static getImageRef(entity: ImageEntity, id: number, size: ImageSize = ImageSettings.thumbnail) {
        //https://github.com/PolymerElements/iron-image/pull/117
        return ServiceSettings.origin + '/image/get?entity=' + entity + '&id=' + id + '&width=' + size.width + (size.height ? '&height=' + size.height : '');
    }

    constructor(public entity: ImageEntity, object?: Image) {
        if (object)
            Object.assign(this, object);
    }
    maxImageSize: ImageSize.Type;
    get hasImage(): boolean {
        return this.maxImageSize > 0 ? true : false;
    }
    imageId: number;
    imageSize: ImageSize;
    get imageRef(): string {
        if (this.hasImage) {
            if (this.imageId)
                return Image.getImageRef(this.entity, this.imageId, this.imageSize);
            else
                return '/images/bizsort-logo.svg'; //throw "ImageId is required";
        }
        else
            return Image.getImageRef(this.entity, 0, this.imageSize);
    }
}

export namespace ImageCollection {
    export interface Image {
        id: number;
        token?: string;
        imageRef?: string;
    }
}

export interface ImageCollection {
    entity: ImageEntity;
    refs: ImageCollection.Image[];
}

export enum ImageEntity {
    Company = 1,
    Product = 2,
    Service = 3,
    Project = 4,
    Job = 5,
    Community = 6,
    CommunityArticle = 7,
    Person = 8,
    Organization = 9,
    Marketplace = 10,
    Promotion = 11,
    Website = 12
}

/*export interface ImageSize {
    Size: number;
    AspectRatio: number;
    Orientation: ImageOrientation;
}*/

export class ImageSize {
    constructor(public width: number, public height: number) {
    }

    /*constructor(width: number, height: number) {
        if (width > height) {
            this.Size = width;
            this.AspectRatio = Math.round((width / height) * 10) / 10; //http://stackoverflow.com/questions/7342957/how-do-you-round-to-1-decimal-place-in-javascript
            this.Orientation = ImageOrientation.Landscape;
        }
        else {
            this.Size = width;
            this.AspectRatio = 1;
            this.Orientation = ImageOrientation.Any;
        }
    }*/
}

export namespace ImageSize {
    export enum Type {
        Thumbnail = 1,
        XtraSmall = 2,
        Small = 3,
        MediumSmall = 4,
        Medium = 5
    }
}

export class WebAppImage {
    static getImageRef(app: string, alias: string, size: WebAppImage.Size) {
        //https://github.com/PolymerElements/iron-image/pull/117
        return ServiceSettings.origin + '/image/get_App?app=' + app + '&alias=' + alias + '&size=' + size;
    }

    constructor(public app: string, public alias: string, public size: WebAppImage.Size, object?: WebAppImage) {
        if (object)
            Object.assign(this, object);
    }

    type: string;
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

export enum PendingStatus {
    EmailConfirmation = 1,
    PeerReview = 2,
    StaffReview = 4
}

export class ProductStats {
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
    Refresh(count) {
        this.Pending = count.Pending;
        this.Active = count.Active;
        this.Inactive = count.Inactive;
        this.Total = count.Total;
    }
    Test() {
        var quota = -1;
        var quotaType;
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

export namespace ProductType {
    export const Default = 16387;
    export enum ItemType {
        Product = 1,
        Service = 2,
        Job = 4,
        Other = 16384
    }
}

export interface ProductType extends DictionaryItem {
}

export enum ProductsView {
    NoProducts = 0,
    Multiproduct = 1,
    ProductList = 2,
    Marketplace = 3,
}

export enum ProjectType {
    Commercial = 1,
    Consumer = 2,
    Tender = 4,
    Default = 3
}

export enum JobType {
    Fulltime = 1,
    Parttime = 2,
    Contract = 4
}

export enum PostType {
    None = 0,
    NoAccount = 1,
    Personal = 2,
    Company = 4
    //Either = 7
}

export interface PreviewOptions {
    imageSize?: ImageSize;
    fetchOptions?: Object;
}

export interface RawImage {
    width: number;
    height: number;
    content: string;
}

export interface ISecurityProfile {
    type: SecurityProfile.Type;
    autoPost: boolean;
    canRelease_Peer: boolean;
    canSuspend: boolean;
    canReview_Staff: boolean;
    canEdit_All: boolean;
    canProduce_Company: boolean;
    canProduce_Product: boolean;
    canManage_OffensiveList: boolean;
    canManage_Categories: boolean;
    canManage_Locations: boolean;
    canManage_Users: boolean;
    canManage_CompanyImport: boolean;
    canManage_ProductImport: boolean;
}

export class SecurityProfile implements ISecurityProfile {
    type: SecurityProfile.Type;
    autoPost: boolean;
    canRelease_Peer: boolean;
    canSuspend: boolean;
    canReview_Staff: boolean;
    canEdit_All: boolean;
    canProduce_Company: boolean;
    canProduce_Product: boolean;
    canManage_OffensiveList: boolean;
    canManage_Categories: boolean;
    canManage_Locations: boolean;
    canManage_Users: boolean;
    canManage_CompanyImport: boolean;
    canManage_ProductImport: boolean;

    constructor() {
        this.reset();
    }

    initialize(securityProfile: ISecurityProfile) {
        this.type = securityProfile.type;
        this.autoPost = securityProfile.autoPost;
        this.canRelease_Peer = securityProfile.canRelease_Peer;
        this.canSuspend = securityProfile.canSuspend;
        this.canReview_Staff = securityProfile.canReview_Staff;
        this.canEdit_All = securityProfile.canEdit_All;
        this.canProduce_Company = securityProfile.canProduce_Company;
        this.canProduce_Product = securityProfile.canProduce_Product;
        this.canManage_OffensiveList = securityProfile.canManage_OffensiveList;
        this.canManage_Categories = securityProfile.canManage_Categories;
        this.canManage_Locations = securityProfile.canManage_Locations;
        this.canManage_Users = securityProfile.canManage_Users;
        this.canManage_CompanyImport = securityProfile.canManage_CompanyImport;
        this.canManage_ProductImport = securityProfile.canManage_ProductImport;
    }

    reset() {
        this.type = 0;
        this.autoPost = false;
        this.canRelease_Peer = false;
        this.canSuspend = false;
        this.canReview_Staff = false;
        this.canEdit_All = false;
        this.canProduce_Company = false;
        this.canProduce_Product = false;
        this.canManage_OffensiveList = false;
        this.canManage_Categories = false;
        this.canManage_Locations = false;
        this.canManage_Users = false;
        this.canManage_CompanyImport = false;
        this.canManage_ProductImport = false;
    }
}

export namespace SecurityProfile {
    export enum Type {
        Low = 1,
        Medium = 2,
        High = 3,
        Affiliate = 4,
        Staff = 5
    }
}

export enum SigninStatus {
    Success = 1,
    AccountLocked = 2
}

export namespace TransactionType {
    export const Default = 7;
    export enum ItemType {
        Company = 1,
        Consumer = 2,
        Sell = 4,
        Lease = 8,
        Buy = 128
    }
}