import { IViewAdapter, Validateable, ViewModel } from '../../../viewmodel'
import { IdName, Autocomplete } from '../../../model/foundation'
import { autocomplete as fetchCategories } from '../../../service/category'
import { Autocomplete as AutocompleteViewModel } from '../../group/autocomplete'

export interface ReflectTokenOptions {
    token?: any;
    fromUser?: boolean;
    callback?: (selected?: any) => void;
}

export class Input extends ViewModel  {
    constructor(view: IViewAdapter) {
        super(view);
        this._validateable = new Validateable(this, null, null, (proceed) => {
            if (!(this._selected && this._selected.id) && !this._text) {
                this.validateable.errorInfo.setError('self', 'Please enter a search criteria');
                proceed(false);
            }
            else {
                this.validateable.errorInfo.clear();
                proceed(true);
            }
        });
    }

    reflectToken(options: ReflectTokenOptions = {}) {
        // Mock token handling for now
        var categoryId = 0; 
        if (!this.selected || this.selected.id != categoryId) {
            if (categoryId) {
                // Fetch logic
            }
            else {
                if (!options.callback)
                    this.selected = this._scope;
                else
                    options.callback(this._scope);
            }
        }
        else if (options.callback)
            options.callback();
    }

    _scope: IdName = {
        id: 0,
        name: 'All Categories'
    };

    _text: string = '';
    get text(): string {
        return this._text;
    }
    set text(text: string) {
        this.validateable.errorInfo.clear();
        if (this._text != text) {
            this._text = text;
            this.notifyView(['text']);
            if (this._autocomplete) {
                console.log('[CategoryInputViewModel] populating autocomplete with text:', text);
                this._autocomplete.populate(text);
            } else {
                console.warn('[CategoryInputViewModel] _autocomplete is null/undefined during set text');
            }
        }
    }

    _selected: IdName = this._scope;
    get selected(): IdName {
        return this._selected;
    }
    set selected(selected: IdName) {
        if (this._selected != selected) {
            this._selected = selected;
            this.notifyView(['selected']);
        }
    }

    resetSelected() {
        this.selected = this._scope;
    }

    protected _autocomplete: AutocompleteViewModel;
    get autocomplete(): AutocompleteViewModel {
        return this._autocomplete;
    }

    public initialized: boolean = false;

    initialize() {
        if (!this.initialized) {
            this._autocomplete = new AutocompleteViewModel(this.view);
            this._autocomplete.initialize({
                master: this,
                populate: (text, callback) => {
                    console.log('[CategoryInputViewModel] Calling fetchCategories API with text:', text);
                    fetchCategories(this._scope ? this._scope.id : 0, text, this._scope)
                        .then(items => {
                            console.log('[CategoryInputViewModel] API returned items:', items);
                            callback(items || []);
                        })
                        .catch(err => {
                            console.error('[CategoryInputViewModel] Failed to fetch categories:', err);
                            callback([]);
                        });
                },
                itemSelected: (category) => {
                    this.text = ''; // Clear Text *before* Selected notify
                    this.selected = category;
                }
            });
            this.initialized = true;
        }
    }
}