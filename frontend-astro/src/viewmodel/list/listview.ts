import { ViewModel, type IViewAdapter } from '../../viewmodel';
import { Event } from '../../global';
import type { EntityId } from '../../model/foundation';

export enum ViewType {
    Card = 1,
    List = 2
}

export enum ItemOption {
    FetchOptIn = 1,
    FetchOptOut = 2,
    DisplayOptOut = 3,
    DisplayOptIn = 4
}

enum ItemFetchOption {
    None = 0,
    In = 1,
    Out = 2
}

enum ItemDisplayOption {
    None = 0,
    In = 1,
    Out = 2
}

interface ItemProperty {
    Fetch: ItemFetchOption;
    Display: ItemDisplayOption;
}

export class ListViewModel extends ViewModel {
    public itemSelectedChanged: Event<EntityId>;

    protected _type: ViewType = ViewType.Card;
    set type(viewType: ViewType) {
        if (this._type !== viewType) {
            this._type = viewType;
            this.notifyView(['type']);
        }
    }
    get type(): ViewType {
        return this._type;
    }

    protected _items: any[] = [];
    set items(items: any[]) {
        if (this._items !== items) {
            this._items = items || [];
            this.notifyView(['items']);
        }
    }
    get items(): any[] {
        return this._items;
    }

    constructor(view: IViewAdapter) {
        super(view);
        this.itemSelectedChanged = new Event<EntityId>();
    }

    initialize(options?: any) {
        // ViewModel base class does not have an initialize method
    }

    protected _itemOptions: { [propertyName: string]: ItemProperty } = {};
    get fetchOptions(): Object {
        const fetchOptions: any = {};
        for (const n in this._itemOptions) {
            const ip = this._itemOptions[n];
            if (ip.Fetch === ItemFetchOption.In && ip.Display !== ItemDisplayOption.Out) {
                fetchOptions[n] = true;
            }
        }
        return fetchOptions;
    }

    protected _displayOptions: any = {};
    protected _cachedDisplayOptions: any = null;
    set displayOptions(displayOptions: Object) {
        this._displayOptions = displayOptions || {};
        this._cachedDisplayOptions = null;
    }
    get displayOptions() {
        if (!this._cachedDisplayOptions) {
            const displayOptions: any = {};
            for (const propertyName in this._displayOptions) {
                displayOptions[propertyName] = this._displayOptions[propertyName];
            }
            for (const propertyName in this._itemOptions) {
                const itemProperty = this._itemOptions[propertyName];
                if (itemProperty.Fetch !== ItemFetchOption.None || itemProperty.Display !== ItemDisplayOption.None) {
                    displayOptions[propertyName] = itemProperty.Display === ItemDisplayOption.In || 
                        (itemProperty.Fetch === ItemFetchOption.In && itemProperty.Display !== ItemDisplayOption.Out) ? true : false;
                }
            }
            this._cachedDisplayOptions = displayOptions;
        }
        return this._cachedDisplayOptions;
    }

    setItemOption(propertyName: string, option: ItemOption) {
        let itemProperty = this._itemOptions[propertyName];
        switch (option) {
            case ItemOption.FetchOptIn:
                if (itemProperty === undefined)
                    this._itemOptions[propertyName] = { Fetch: ItemFetchOption.In, Display: ItemDisplayOption.In };
                break;
            case ItemOption.FetchOptOut:
                if (itemProperty === undefined)
                    this._itemOptions[propertyName] = { Fetch: ItemFetchOption.Out, Display: ItemDisplayOption.None };
                break;
            case ItemOption.DisplayOptIn:
                if (itemProperty === undefined)
                    this._itemOptions[propertyName] = { Fetch: ItemFetchOption.None, Display: ItemDisplayOption.In };
                else if (itemProperty.Display !== ItemDisplayOption.In)
                    itemProperty.Display = ItemDisplayOption.In;
                break;
            case ItemOption.DisplayOptOut:
                if (itemProperty === undefined)
                    this._itemOptions[propertyName] = { Fetch: ItemFetchOption.None, Display: ItemDisplayOption.Out };
                else if (itemProperty.Display !== ItemDisplayOption.Out)
                    itemProperty.Display = ItemDisplayOption.Out;
                break;
        }
        this._cachedDisplayOptions = null;
        this.notifyView(['displayOptions']);
    }
}
