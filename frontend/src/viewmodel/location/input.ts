import { ErrorInfo, IViewAdapter, Validateable, ViewModel, ErrorMessageType } from '../../viewmodel'
import { ResolvedLocation } from '../../model/foundation'

export interface IInput extends ViewModel {
    validateable: Validateable;
    text: string;
    reset: () => void;
}

export class Input extends ViewModel implements IInput {
    constructor(view: IViewAdapter) {
        super(view);
        this._geoinput = new GeocoderInput(this);
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
            return true;
        }
        return false;
    }

    reset() {
        this._geoinput.geoCoded = null;
    }

    get geoLocation(): any {
        return this._geoinput.geoValidated;
    }

    errorMessage(error: ErrorMessageType, data: any, options?: any) {
        return "An error occurred"; // Simplified for now
    }
}

export class GeocoderInput {
    public errorInfo: ErrorInfo;
    public geoCoded: any = null;
    public geoValidated: any = null;
    protected _autocomplete: google.maps.places.Autocomplete | null = null;
    
    constructor(public viewModel: ViewModel) {
        this.errorInfo = new ErrorInfo(viewModel.validateable);
    }

    initAutocomplete(inputElement: any, types: string[] = ['geocode']) {
        if (typeof google !== 'undefined' && google.maps && google.maps.places) {
            this._autocomplete = new google.maps.places.Autocomplete(inputElement, { types });
            this._autocomplete.addListener('place_changed', () => {
                const place = this._autocomplete?.getPlace();
                if (place && place.geometry) {
                    this.geoValidated = {
                        id: 0,
                        name: place.formatted_address || place.name,
                        geometry: place.geometry.location?.toJSON()
                    };
                    this.viewModel.notifyView(['geoLocation']);
                }
            });
        }
    }
}