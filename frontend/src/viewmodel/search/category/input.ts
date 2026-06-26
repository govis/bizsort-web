import { IViewAdapter, Validateable, ViewModel } from '../../../viewmodel'
import { IdName, Autocomplete } from '../../../model/foundation'
import { autocomplete as fetchCategories } from '../../../service/category'

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

    async fetchSuggestions(query: string): Promise<Autocomplete[]> {
        return await fetchCategories(this._scope ? this._scope.id : 0, query, this._scope);
    }
}