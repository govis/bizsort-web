import { IViewAdapter, Validateable, ViewModel } from '../../viewmodel';
import { Input as CategoryInput } from './category/input';
import { Input as LocationInput } from '../location/input';

export interface Selection {
    category: number;
    location: number;
    query?: string;
    near?: any;
}

export class SearchHome$ extends ViewModel {
    protected _category!: CategoryInput;
    protected _location!: LocationInput;

    protected _selection!: Selection;
    set selection(selection: Selection) {
        if (selection && (!this._selection || 
            this._selection.category !== selection.category || 
            this._selection.location !== selection.location ||
            this._selection.query !== selection.query ||
            JSON.stringify(this._selection.near) !== JSON.stringify(selection.near)
        )) {
            this._selection = selection;
            this.notifyView(['selection']);
        }
    }
    get selection(): Selection {
        return this._selection;
    }

    constructor(view: IViewAdapter) {
        super(view);
        this._validateable = new Validateable(this, null, null, (proceed) => {
            const categoryValid = this._category ? this._category.validateable.validate() : true;
            const locationValid = this._location ? this._location.validateable.validate() : true;
            
            if (categoryValid && locationValid) {
                this.reflectSelection();
                proceed(true);
            } else {
                proceed(false);
            }
        });
    }

    public attachInputs(category: CategoryInput, location: LocationInput) {
        this._category = category;
        this._location = location;
        if (this._selection) {
            this.loadSelection(this._selection);
        }
    }

    public loadSelection(selection: Selection) {
        this._selection = selection;
        if (!this._category || !this._location) return;

        if (selection.category) {
            this._category.reflectToken(selection.category);
        } else {
            this._category.resetSelected();
        }
        
        this._category.text = selection.query || '';

        if (selection.location) {
            this._location.reflectToken(selection.location);
        } else {
            this._location.reset();
        }
        
        this._location.text = selection.near ? selection.near.text : '';
    }

    validate(): boolean {
        return this.validateable.validate();
    }

    public reflectSelection() {
        if (this._category && this._location) {
            let near = undefined;
            if ((this._location as any).geoMode && (this._location as any).geoLocation) {
                const geo = (this._location as any).geoLocation;
                if (geo.geometry) {
                    near = {
                        text: this._location.text,
                        lat: geo.geometry.lat,
                        lng: geo.geometry.lng
                    };
                }
            }

            this.selection = {
                category: this._category.selected ? this._category.selected.id : 0,
                location: this._location.selected ? this._location.selected.id : 0,
                query: this._category.text || undefined,
                near: near
            };
        }
    }
}
