import { List, Semantic } from '../../model/foundation';
import type { EntityId } from '../../model/foundation';
import { ViewModel, IViewAdapter } from '../../viewmodel';
import { Event } from '../../global';
import type { Constructor, IEventHandler, Action } from '../../global';

// Mock types for components not yet ported
export interface ListView { type?: number; items?: any[]; setItemOption?: (name: string, option: any) => void; }
export interface ListHeader { data?: Header.Data; swapFormat?: (a: boolean, b: boolean) => void; view?: any; }
export interface FilterAvailable { filterSelected?: (facet: Semantic.Facet) => void; populate?: (facets: any) => void; }
export interface FilterApplied { filterSelected?: (facet: Semantic.Facet) => void; add?: (facet: Semantic.Facet) => void; remove?: (facet: Semantic.Facet) => void; clear?: () => void; facets?: Semantic.Facet[]; }
export enum ViewType { List = 1, Grid = 2, Card = 3 }
export enum ItemOption { DisplayOptIn = 1, DisplayOptOut = 2 }

export type { Action, EntityId }

export function Filterable<T extends Constructor<View>>(superclass: T) {
    abstract class FilterableClass extends superclass {
        protected _filterAvail!: FilterAvailable;
        protected _filterApplied!: FilterApplied;
        knownFacetNames: string[] = [];

        public initialize(options?: IViewInitialize) {
            super.initialize(options);
            // In a modernized setup, we would retrieve these via query selectors on the host element or pass them in
            this._filterAvail = (this.view as any).getViewModel?.('filterAvail');
            this._filterApplied = (this.view as any).getViewModel?.('filterApplied');
            
            if (this._filterAvail && this._filterApplied) {
                this._filterAvail.filterSelected = this.applyFilter.bind(this);
                this._filterApplied.filterSelected = this.removeFilter.bind(this);
            }
        }

        protected populate(pageIndex: number, ...args: any[]) {
            if (this._filterApplied) {
                const queryInput = new List.Filter.QueryInput(this._filterApplied.facets || []);
                queryInput.startIndex = (pageIndex > 0 ? this._pager.fetchIndex : 0);
                queryInput.length = this._pager.fetchLimit;
                this._fetchPending = true;
                this._pager.canChangePage = false;
                
                this.fetchList(queryInput, (data: any) => {
                    this._fetchPending = false;
                    switch (this._pager.populate(data, pageIndex)) {
                        case PopulateStatus.Buffer_Initialized:
                            if (this._filterAvail && this._filterAvail.populate) this._filterAvail.populate(data.facets);
                            this.formatActions(data.series.length == 0 ? true : false);
                            break;
                        case PopulateStatus.Empty:
                            if (this._filterAvail && this._filterAvail.populate) this._filterAvail.populate([]);
                            this.setEmpty();
                            break;
                    }
                }, (ex: any) => {
                    this._fetchPending = false;
                    console.error("Filterable fetch error", ex);
                });
            } else {
                super.populate(pageIndex, ...args);
            }
        }

        applyFilter(facet: Semantic.Facet) {
            if (!this._fetchPending) {
                this._fetchPending = true;
                if (this._filterApplied && this._filterApplied.add) this._filterApplied.add(facet);
                if (!facet.exclude && this.knownFacetNames.indexOf(facet.nameText) >= 0 && this.listView && this.listView.setItemOption)
                    this.listView.setItemOption(facet.nameText, ItemOption.DisplayOptOut);
                this.populate(0);
            }
        }

        removeFilter(facet: Semantic.Facet) {
            if (!this._fetchPending) {
                this._fetchPending = true;
                if (this._filterApplied && this._filterApplied.remove) this._filterApplied.remove(facet);
                if (!facet.exclude && this.knownFacetNames.indexOf(facet.nameText) >= 0 && this.listView && this.listView.setItemOption)
                    this.listView.setItemOption(facet.nameText, ItemOption.DisplayOptIn);
                this.populate(0);
            }
        }

        load(...args: any[]) {
            if (this._filterApplied && this._filterApplied.clear) this._filterApplied.clear();
            super.load(...args);
        }

        search(...args: any[]) {
            if (this._filterApplied && this._filterApplied.clear) this._filterApplied.clear();
            super.search(...args);
        }
    }
    return FilterableClass;
}

export interface IHeader {
    totalCount: number;
    fromRecord?: number;
    toRecord?: number;
}

export interface IViewInitialize {
    listView?: ListView;
    listHeader?: ListHeader;
}

export namespace Header {
    export class Data {
        totalCount!: number;
        fromRecord!: number;
        toRecord!: number;
        query!: string;
        folder!: string;

        constructor(data: IHeader) {
            Object.assign(this, data);
        }

        get isEmpty() {
            return this.fromRecord && this.fromRecord > 0 && this.toRecord && this.toRecord > 0 ? false : true;
        }

        toArray() {
            var a = [];
            for (var i in this)
                if (!(typeof this[i as keyof this] == 'function' || i == 'format'))
                    a.push(this[i as keyof this]);
            return a;
        }
    }
}

export enum PopulateStatus {
    Empty = 0,
    Buffer_Initialized = 1,
    Buffer_Expanded = 2
}

export enum AddItems {
    Replace = 0,
    Append = 1,
    Prepend = 2
}

export interface PopulatePageOptions {
    addItems?: AddItems
}

interface notifyPageOptions extends PopulatePageOptions {
    setPage: boolean
}

export class Pager {
    isPageChanging = false;
    totalItemCount = -1;

    fromRecord = -1;
    toRecord = -1;
    _propertyChange: Event<string>;
    observeProperty(observer: IEventHandler<string>): () => void {
        this._propertyChange.subscribe(observer);
        return () => this._propertyChange.unsubscribe(observer);
    }

    pageSizeOptions = [12, 24, 48, 96];

    protected _buffer: any[] | null = null;
    protected _fetchCount = 0;
    get fetchIndex(): number {
        return this._buffer ? this._buffer.length : 0;
    }
    reset() {
        if (this._buffer || this._fetchCount || this.pageIndex !== -1) {
            this._buffer = null;
            this._fetchCount = 0;
            this.itemCount = 0;
            this.pageIndex = -1;
        }
    }

    populatePage?: Action<EntityId[], PopulatePageOptions>;
    populateBuffer?: Action<number>;

    pageChanging?: Action<number>;
    pageChanged?: Action<void>;

    constructor() {
        this._propertyChange = new Event<string>();
    }

    protected _canChangePage = false;
    get canChangePage(): boolean {
        return this._canChangePage;
    }
    set canChangePage(canChangePage: boolean) {
        if (this._canChangePage != canChangePage) {
            this._canChangePage = canChangePage;
            this.notifyProperty(['canChangePage']);
        }
    }

    protected _itemCount = 0;
    get itemCount(): number {
        return this._itemCount;
    }
    set itemCount(itemCount: number) {
        if (this._itemCount != itemCount) {
            this._itemCount = itemCount;
            this.notifyProperty(['itemCount']);
        }
    }

    protected _pageIndex = -1;
    get pageIndex(): number {
        return this._pageIndex;
    }
    set pageIndex(pageIndex: number) {
        if (this._pageIndex != pageIndex) {
            this._pageIndex = pageIndex;
            this.notifyProperty(['pageIndex']);
        }
    }

    protected _pageSize = 12;
    get pageSize(): number {
        return this._pageSize > 0 ? this._pageSize : 12;
    }
    set pageSize(pageSize: number) {
        if (this._pageSize != pageSize) {
            this._pageSize = pageSize;
            this._populatePage(0, {
                setPage: true
            });
        }
    }

    notifyProperty(name: string[]) {
        name.forEach(n => {
            this._propertyChange.trigger(this, n);
        });
    }

    hasPage(pageIndex: number) {
        return (pageIndex >= 0 && this.itemCount > 0 && pageIndex < this.pageCount) ? true : false;
    }

    moveToPage(pageIndex: number, addItems?: AddItems) {
        if (this.canChangePage && this.hasPage(pageIndex) && pageIndex != this.pageIndex) {
            if (this.fetchLimit > 0 && this.fetchIndex > 0 && ((pageIndex + 1) * this.pageSize) > this._fetchCount) {
                if (this.populateBuffer)
                    this.populateBuffer(pageIndex);
                else
                    throw new Error('Pager integrity check failed: moveToPage');
            }
            else
                return this._populatePage(pageIndex, {
                    setPage: true,
                    addItems: addItems || AddItems.Replace
                }) > 0;
        }

        return false;
    }

    moveToPreviousPage() {
        return this.moveToPage(this.pageIndex - 1);
    }

    moveToNextPage(addItems?: AddItems) {
        return this.moveToPage(this.pageIndex + 1, addItems);
    }

    moveToFirstPage() {
        return this.moveToPage(0);
    }

    moveToLastPage() {
        return this.moveToPage(this.pageCount - 1);
    }

    get pageCount(): number {
        return this.itemCount > 0 ? Math.max(1, Math.ceil(this.itemCount / this.pageSize)) : 0;
    }

    notifyPopulatePage(pageIndex: number, page: EntityId[] | null, options: notifyPageOptions) {
        if (options.setPage) {
            this.isPageChanging = true;
            if (this.pageChanging)
                this.pageChanging(pageIndex);

            this.pageIndex = pageIndex;
        }
        else if (this.pageIndex != pageIndex)
            throw new Error("Unexpected State");

        if (this.populatePage && page)
            this.populatePage(page, {
                addItems: options.addItems
            });

        if (options.setPage) {
            this.isPageChanging = false;
            if (this.pageChanged)
                this.pageChanged();
        }
    }

    _populatePage(pageIndex: number, options: notifyPageOptions) {
        if (this._buffer && this._buffer.length > 0 && pageIndex >= 0) {
            var startIndex = pageIndex * this.pageSize;
            var page = this._buffer.slice(startIndex, startIndex + this.pageSize);

            if (!options.addItems) {
                this.fromRecord = startIndex + 1;
                this.toRecord = startIndex + page.length;
            }
            else if (this.fromRecord == 1)
                this.toRecord += page.length;

            this.notifyPopulatePage(pageIndex, page, options);

            return page.length;
        }
        return 0;
    }

    refreshPage() {
        if (this.pageIndex >= 0)
            this._populatePage(this.pageIndex, {
                setPage: false
            });
    }

    protected _fetchLimit = 0;
    get fetchLimit(): number {
        return this._fetchLimit;
    }
    set fetchLimit(fetchLimit: number) {
        if (this._fetchLimit != fetchLimit) {
            this._fetchLimit = fetchLimit;
        }
    }

    populate(data: List.QueryOutput, pageIndex: number) {
        this.canChangePage = false;
        var populateStatus;
        if (!this._buffer || !data || data.startIndex === undefined || data.startIndex === 0) {
            if (data && data.series && data.series.length > 0) {
                this._buffer = data.series;
                this._fetchCount = (this._fetchLimit > 0 ? this._fetchLimit : this._buffer.length);

                if (data.totalCount) {
                    this.itemCount = data.totalCount;
                }
                else {
                    this.itemCount = data.series.length;
                }
                if ((pageIndex * this.pageSize) >= data.series.length)
                    pageIndex = 0;
                populateStatus = PopulateStatus.Buffer_Initialized;
            }
            else {
                this._fetchCount = 0;
                this._buffer = null;
                this.itemCount = 0;
                pageIndex = -1;
                populateStatus = PopulateStatus.Empty;
            }
        }
        else if (this._buffer.length == data.startIndex && pageIndex == this.pageIndex + 1) {
            this._fetchCount = this._buffer.length + this._fetchLimit;

            if (data.series.length > 0) {
                this._buffer.push.apply(this._buffer, data.series)
            }
            else
                pageIndex = this.pageIndex;
            populateStatus = PopulateStatus.Buffer_Expanded;
        }
        else
            throw new Error('Pager integrity check failed: Populate');

        if (pageIndex < 0) {
            this.fromRecord = -1;
            this.toRecord = -1;

            this.notifyPopulatePage(pageIndex, null, {
                setPage: true
            });
        }
        else if (pageIndex == 0 || pageIndex != this.pageIndex) {
            this._populatePage(pageIndex, {
                setPage: true
            });
        }

        return populateStatus;
    }
}

export abstract class View extends ViewModel
{
    listHeader?: ListHeader;
    listView?: ListView;
    get viewType(): ViewType | undefined {
        return this.listView && this.listView.type;
    }

    protected _fetchPending = false;

    protected abstract fetchList(queryInput: List.QueryInput | List.Filter.QueryInput, callback: Action<List.QueryOutput | List.Filter.QueryOutput>, faultCallback: Action<any>, arg1?: any): void;

    _pager: Pager;
    get pager(): Pager {
        return this._pager;
    }

    set listItems(items: any) {
        if (this.listView)
            this.listView.items = items;
        this.clearSelected();
    }

    addItems(items: any, addItems: AddItems) {
        if (!this.listView) return;
        if (addItems == AddItems.Prepend)
            this.listView.items = [...items, ...(this.listView.items || [])];
        else
            this.listView.items = [...(this.listView.items || []), ...items];
    }

    constructor(view: IViewAdapter) {
        super(view);
        this._pager = new Pager();
        this._pager.populateBuffer = this.populate.bind(this);
        this._pager.populatePage = this.populatePage.bind(this);
        this._pager.observeProperty((sender, propertyName) => {
            if (propertyName == 'pageIndex' || propertyName == 'itemCount') {
                this.showMore = (this._pager.pageIndex >= 0 && this._pager.hasPage(this._pager.pageIndex + 1) ? true : false);
            }
        });
    }

    initialize(options: IViewInitialize = {}) {
        this.listView = options.listView || (this.view as any).getViewModel?.('listView');
        this.listHeader = options.listHeader || (this.view as any).getViewModel?.('listHeader');
    }

    load(...args: any[]) {
        if (this.validateable && this.validateable.errorInfo) {
           this.validateable.errorInfo.clear();
        }
        this._pager.reset();
        this.populate(0);
    }

    search(...args: any[]) {
        this._pager.reset();
        this.populate(0);
    }

    protected populate(pageIndex: number, ...args: any[]) {
        var queryInput: List.QueryInput = {
            startIndex: (pageIndex > 0 ? this._pager.fetchIndex : 0),
            length: this._pager.fetchLimit
        };
        this._fetchPending = true;
        this._pager.canChangePage = false;
        this.fetchList(queryInput, (data) => {
            this._fetchPending = false;
            switch (this._pager.populate(data, pageIndex)) {
                case PopulateStatus.Buffer_Initialized:
                    this.formatActions(data.series.length == 0 ? true : false);
                    break;
                case PopulateStatus.Empty:
                    this.setEmpty();
                    break;
            }
        }, (ex) => {
            this._fetchPending = false;
            console.error(ex);
        });
    }

    protected populatePage(page: EntityId[], options: PopulatePageOptions) {
        if (page && page.length > 0) {
            if (this.listHeader)
                this.listHeader.data = this.populateHeader(new Header.Data({ fromRecord: this._pager.fromRecord, toRecord: this._pager.toRecord, totalCount: this._pager.itemCount }));
            this._pager.canChangePage = false;
            
            this._fetchPending = true;
            this.fetchPage(page, (data) => {
                this._fetchPending = false;
                if (!options.addItems)
                    this.listItems = data;
                else
                    this.addItems(data, options.addItems);
                this._pager.canChangePage = true;
            }, (ex) => {
                this._fetchPending = false;
                console.error(ex);
            });
        } else {
            if (this.listHeader)
                this.listHeader.data = this.populateHeader(new Header.Data({ totalCount: 0 }));
            this.listItems = [];
        }
    }

    protected preparePage(refs: EntityId[], items: EntityId[], valueSetter?: any, pager?: any): Object[] {
        if (items && items.length > 0 && items.length <= refs.length) {
            var item: any;
            var sorted = [];
            pager = pager || this._pager;
            for (var i = 0, l = refs.length; i < l; i++) {
                if (items[i].id == refs[i].id) {
                    item = items[i];
                }
                else {
                    item = null;
                    for (var j = 0, l2 = items.length; j < l2; j++) {
                        if (items[j].id == refs[i].id) {
                            item = items[j];
                            break;
                        }
                    }
                }
                if (item) {
                    if (valueSetter)
                        valueSetter(item, refs[i]);
                    sorted.push(item);
                }
            }
            return sorted;
        }
        return items;
    }

    protected populateHeader(header: any) {
        return header;
    }

    protected _selectedItems: EntityId[] = [];
    get selectedItems(): EntityId[] {
        return this._selectedItems;
    }
    set selectedItems(selectedRequests: EntityId[]) {
        if (this._selectedItems != selectedRequests) {
            this._selectedItems = selectedRequests;
            this.notifyProperty(['selectedItems']);
        }
    }
    
    notifyProperty(names: string[]) {
        this.notifyView(names);
    }
    
    clearSelected() {
        if (this.selectedItems.length) {
            this._selectedItems.length = 0;
            this.notifyProperty(['selectedItems']);
        }
    }

    selectItem(item: any) {
        if (item && this._selectedItems.indexOf(item) == -1) {
            this._selectedItems.push(item);
            this.notifyProperty(['selectedItems']);
        }
    }

    deselectItem(item: any) {
        var index = item && this._selectedItems.indexOf(item);
        if (index >= 0) {
            this._selectedItems.splice(index, 1);
            this.notifyProperty(['selectedItems']);
        }
    }

    protected setEmpty() {
        this.formatActions(true);
    }

    protected _showMore = false;
    get showMore(): boolean {
        return this._showMore;
    }
    set showMore(showMore: boolean) {
        if (this._showMore != showMore) {
            this._showMore = showMore;
            this.notifyProperty(['showMore']);
        }
    }

    protected formatActions(isEmpty?: boolean) {
    }

    abstract fetchPage(page: EntityId[], fetchAction: Action<Object[]>, faultCallback: Action<any>): void;
}

export abstract class Searchview extends View {
    // Pass these via options or properties instead of a global Page.token
    searchParams?: {
        categoryId?: number;
        searchQuery?: string;
        searchNear?: any;
        locationId?: number;
    };

    fetchList(queryInput: List.SearchInput, callback: Action<List.SearchOutput>, faultCallback: Action<any>, fetchDelegate?: any) {
        if (this.searchParams) {
            queryInput.category = this.searchParams.categoryId as number;
            if (this.searchParams.searchQuery)
                queryInput.searchQuery = this.searchParams.searchQuery;

            if (this.searchParams.searchNear) {
                queryInput.searchNear = this.searchParams.searchNear;
                if (fetchDelegate) {
                    fetchDelegate(queryInput, (queryOutput: any) => {
                        if (queryOutput.series && queryOutput.series.length) {
                            if (typeof queryOutput.series[0].distance === 'undefined')
                                console.warn('Proximity search did not return distance values');
                        }
                        callback(queryOutput);
                    }, faultCallback);
                }
            }
            else {
                queryInput.location = this.searchParams.locationId as number;
                if (fetchDelegate) {
                    fetchDelegate(queryInput, callback, faultCallback);
                }
            }
        } else if (fetchDelegate) {
            fetchDelegate(queryInput, callback, faultCallback);
        }
    }
}
