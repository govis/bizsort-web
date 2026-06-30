import { ViewModel } from '../../viewmodel';

export interface InitOptions {}
import { IdName, Autocomplete as AutocompleteModel } from '../../model/foundation';

export interface IAutocompleteMaster extends ViewModel {
    text: string;
}

export interface IAutocompletePopulate {
    (text: string, callback: (items: AutocompleteModel[]) => void): void;
}

export interface AutocompleteInitOptions extends InitOptions {
    master: IAutocompleteMaster;
    populate: IAutocompletePopulate;
    itemSelected: (item: AutocompleteModel) => void;
}

export class Autocomplete extends ViewModel {
    itemSelected!: (item: AutocompleteModel) => void;
    protected _text: string = '';
    protected _populate!: IAutocompletePopulate;

    initialize(options: AutocompleteInitOptions) {
        if (options && options.master && options.populate && options.itemSelected) {
            this._populate = options.populate;
            this.itemSelected = options.itemSelected;
            // Note: The master is responsible for calling populate(text) explicitly
            // since observeProperty is not implemented in the modern ViewModel.
        } else {
            throw new Error('Missing initialize arguments');
        }
    }

    populate(text: string) {
        if (this._text !== text) {
            this._text = text;
            if (this._text) {
                this._populate(this._text, (items) => {
                    if (items && items.length && (items.length > 1 || items[0].name !== this._text)) {
                        this.items = items;
                    } else {
                        this.items = [];
                    }
                });
            } else {
                this.items = [];
            }
        }
    }

    protected _items: AutocompleteModel[] = [];
    get items(): AutocompleteModel[] {
        return this._items;
    }
    set items(items: AutocompleteModel[]) {
        if (this._items !== items) {
            this._items = items;
            this.notifyView(['items']);
        }
    }

    protected _active: boolean = false;
    get active(): boolean {
        return this._active;
    }
    set active(active: boolean) {
        if (this._active !== active) {
            this._active = active;
            this.notifyView(['active']);
        }
    }
}
