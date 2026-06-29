export interface Location {
  address: string;
  geoLocation?: {
    lat: number;
    lng: number;
  };
}

export interface Category {
  id: number;
  name: string;
}

export interface Office {
  id: number;
  name: string;
  phone: string;
  phone1?: string;
  fax?: string;
  location?: Location;
}

export interface Offerings {
  view: number;
  multiProduct?: string;
  label?: string;
  hideOfferings: boolean;
}

export interface ImageRef {
  entity: number;
  maxImageSize?: number;
  imageId?: number;
}

export interface PageConfig {
  label?: string;
}

export interface PageNews extends PageConfig {
  community?: number;
}

export interface PageArticles extends PageConfig {
  community?: number;
  defaultCategory?: string;
}

export interface PageJobs extends PageConfig {
  organization?: number;
  defaultDepartment?: string;
}

export interface PageMarketplace extends PageConfig {
  marketplace?: number;
}

export interface Company {
  id: number;
  name: string;
  email?: string;
  webSite?: string;
  text?: string;
  description?: string;
  richText?: string;
  appUri?: string;
  image?: ImageRef;
  category?: Category;
  headOffice?: Office;
  offices: Office[];
  offerings?: Offerings;
  hasAffiliations?: boolean;
  hasCommunities?: boolean;
  news?: PageNews;
  articles?: PageArticles;
  jobs?: PageJobs;
  marketplace?: PageMarketplace;
  projects?: PageConfig;
  promotions?: PageConfig;
  accountType?: number;
}

// Preview card types (for featured/search results)
export interface CompanyPreview {
  id: number;
  name: string;
  image?: ImageRef;
  location?: Location;
  webSite?: string;
  phone?: string;
  text?: string;
  productsView?: number;
  category?: Category;
}

// List/slice types (for paginated queries)
export interface SliceInput {
  index: number;
  length: number;
  category?: number;
  location?: number;
  skip?: number[];
}

export interface SliceOutput<T> {
  series: T[];
  index: number;
}

export interface QueryOutput<T> {
  startIndex: number;
  series: T[];
  totalCount: number;
}

export interface SearchItem {
  id: number;
  office?: number;
  distance?: number;
}
