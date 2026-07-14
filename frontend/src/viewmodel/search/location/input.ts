import { ErrorInfo, IViewAdapter, Validateable, ViewModel, ErrorMessageType } from '../../../viewmodel'
import { ResolvedLocation } from '../../../model/foundation'

export interface IInput extends ViewModel {
    validateable: Validateable;
    text: string;
    reset: () => void;
}

import { Autocomplete as AutocompleteViewModel } from '../../group/autocomplete'
import { autocomplete as fetchLocations, get as getLocation } from '../../../service/location'
import { Location as LocationSettings } from '../../../settings'
import { IdName } from '../../../model/foundation'

export class Input extends ViewModel implements IInput {
    public initialized: boolean = false;
    protected _autocomplete!: AutocompleteViewModel;
    get autocomplete(): AutocompleteViewModel {
        return this._autocomplete;
    }

    protected _scope: IdName = { id: 0, name: 'Everywhere' };

    constructor(view: IViewAdapter) {
        super(view);
        this._geoinput = new GeocoderInput(this);

        this._validateable = new Validateable(this, null, null, (proceed: (valid: boolean) => void) => {
            if (this._geoMode) {
                this._geoinput.validate((valid: boolean) => {
                    if (valid) {
                        //Resolve Location.id on the server
                        this._geoinput.resolve(false, (match: boolean, location: any) => {
                            proceed(match || location ? true : false);
                        });
                    }
                    else
                        proceed(false);
                });
            }
            else if (!this._selected || !this._selected.id) {
                this.validateable.errorInfo.setError(this._geoinput.errorElementName, "Please select a location from the list");
                proceed(false);
            }
            else {
                if (this._selected.name && this.text != this._selected.name)
                    this.setText(this._selected.name);
                proceed(true);
            }
        });

        if (LocationSettings.country && LocationSettings.country.id) {
            if (LocationSettings.country.name) {
                this._scope = {
                    id: LocationSettings.country.id,
                    name: LocationSettings.country.name
                };
            }
            else {
                getLocation(LocationSettings.country.id).then((country) => {
                    this._scope = {
                        id: country.id,
                        name: country.name
                    };
                }).catch(err => console.error("Failed to fetch location country", err));
            }
        }
    }

    initialize() {
        if (!this.initialized) {
            this._autocomplete = new AutocompleteViewModel(this.view);
            this._autocomplete.initialize({
                master: this,
                populate: (text, callback) => {
                    fetchLocations((this._scope ? this._scope.id : 0), text, this._scope)
                        .then(items => callback(items || []))
                        .catch(err => {
                            console.error('Failed to fetch locations:', err);
                            callback([]);
                        });
                },
                itemSelected: (location) => {
                    this.selected = location;
                }
            });
            this.initialized = true;
        }
    }

    initAutocomplete(inputElement: any, types?: any) {
        this._geoinput.initAutocomplete(inputElement, types);
    }

    protected _geoinput: GeocoderInput;
    get geoInput(): GeocoderInput {
        return this._geoinput;
    }

    protected _text: string = '';
    get text(): string {
        return this._text;
    }
    set text(text: string) {
        if (this.setText(text))
            this.reset();
    }

    protected setText(text: string) {
        if (this._text != text) {
            this._text = text;
            this.notifyView(['text']);
            if (this._autocomplete) {
                this._autocomplete.populate(text);
            }
            return true;
        }
        return false;
    }

    reset() {
        this._geoinput.geoCoded = null;
        this._selected = this._scope;
    }

    protected _selected: any = null;
    get selected(): any {
        return this._selected;
    }
    set selected(value: any) {
        if (this._selected != value) {
            if (!this._geoMode && value && value.name)
                this.setText(value.name);
            const old = {
                selected: this._selected
            };
            this._selected = value;
            this.notifyView(['selected']);
        }
    }

    get geoLocation(): any {
        return this._geoinput.geoValidated;
    }

    protected _geoMode: boolean = false;
    get geoMode(): boolean {
        return this._geoMode;
    }
    set geoMode(geoMode: boolean) {
        if (this._geoMode != geoMode) {
            this._geoMode = geoMode;
            this.reflectGeoMode();
            this.notifyView(['geoMode']);
        }
    }

    geoInit(inputElement: any, types?: any) {
        if (this._geoMode && !this._geoinput.autocompleteObj)
            this._geoinput.initAutocomplete(inputElement, types);
        else if (inputElement && !this._geoinput.inputElement)
            this._geoinput.inputElement = inputElement;
    }

    protected reflectGeoMode() {
        if (this._geoMode) {
            if (this._geoinput.geoCoded)
                this.setText(this._geoinput.geoCoded.text);

            this.geoInit(this._geoinput.inputElement);
        }
        else {
            this._geoinput.clearAutocomplete();

            if (this.selected)
                this.setText(this.selected.name);

            if (this.autocomplete)
                this.autocomplete.populate(this.text);
        }
    }

    reflectToken(locationId: number, options: any = {}) {
        if (!this.selected || this.selected.id !== locationId) {
            if (locationId) {
                import('../../../service/location').then(m => {
                    m.get(locationId).then(location => {
                        this.selected = {
                            id: location.id,
                            name: location.name
                        };
                        if (options.callback) options.callback(this.selected);
                    }).catch(err => {
                        console.error('Failed to fetch location', err);
                        if (options.callback) options.callback();
                    });
                }).catch(err => {
                    if (options.callback) options.callback();
                });
            } else {
                this.selected = this._scope;
                if (options.callback) options.callback(this._scope);
            }
        } else if (options.callback) {
            options.callback(this.selected);
        }
    }

    errorMessage(error: ErrorMessageType, data: any, options?: any) {
        return "An error occurred"; // Simplified for now
    }
}

export class GeocoderInput {
    public errorInfo: ErrorInfo;
    public geoCoded: any = null;
    public geoValidated: any = null;
    public inputElement: any = null;
    public errorElementName: string = 'locationInput';
    protected _autocomplete: google.maps.places.Autocomplete | null = null;
    
    get autocompleteObj(): google.maps.places.Autocomplete | null {
        return this._autocomplete;
    }

    constructor(public viewModel: ViewModel) {
        this.errorInfo = new ErrorInfo(viewModel.validateable);
    }

    validate(callback: (valid: boolean) => void) {
        // Ported stub: check if geocoded data is valid
        if (this.geoCoded || this.geoValidated || (this.inputElement && this.inputElement.value)) {
            callback(true);
        } else {
            this.errorInfo.setError(this.errorElementName, "Please enter a valid location");
            callback(false);
        }
    }

    resolve(flag: boolean, callback: (match: boolean, location: any) => void) {
        // Ported stub: normally hits /api/location/resolve with Google Places ID
        // For now, immediately resolve with the geoValidated location
        callback(true, this.geoValidated);
    }

    initAutocomplete(inputElement: any, types: string[] = ['geocode']) {
        this.inputElement = inputElement;
        // The modern architecture fully delegates Google Maps initialization
        // to the Lit View component (SearchLocationInput).
        // This ViewModel stub remains to preserve interface compatibility.
    }

    clearAutocomplete() {
        // Handled by Lit View
    }

}