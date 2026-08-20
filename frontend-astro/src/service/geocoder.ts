import { apiFetch } from './api.js';
import type { Geocoder } from '../model/foundation';

export interface AddressOptions {
    country?: boolean;
    county?: boolean;
    streetAddress?: boolean;
    postalCode?: boolean;
    address1?: boolean;
}

const LocationSettings = {
    address1Threshold: 10,
    country: { id: 1, name: 'Canada' } // Mocked for now, can be wired to actual settings later
};

export function stringify(address: Geocoder.Address | string | undefined, options: AddressOptions = {}): string {
    if (!address) return '';
    if (typeof address === 'string') return address;

    const parts = [];
    const country = (options.country !== true && LocationSettings.country.name) || '';
    
    if (address.city) {
        if (address.streetName && options.streetAddress !== false) {
            if (address.streetNumber) {
                if (address.address1 && options.address1 !== false) {
                    if (address.address1.length <= LocationSettings.address1Threshold)
                        parts.push(address.streetNumber + ' ' + address.streetName + ' ' + address.address1);
                    else
                        parts.push(address.address1 + ' ' + address.streetNumber + ' ' + address.streetName);
                }
                else
                    parts.push(address.streetNumber + ' ' + address.streetName);
            }
            else {
                if (address.address1 && options.address1 !== false)
                    parts.push(address.address1 + ' ' + address.streetName);
                else
                    parts.push(address.streetName);
            }
            parts.push(address.city);
        }
        else if (address.address1 && options.address1 !== false)
            parts.push(address.address1 + ' ' + address.city);
        else
            parts.push(address.city);
    }
    else if (address.streetName) {
        if (address.streetNumber) parts.push(address.streetNumber + ' ' + address.streetName);
        else parts.push(address.streetName);
    }

    if (address.state) {
        if (address.county && options.county !== false)
            parts.push(address.county);
        if (address.postalCode && options.postalCode !== false)
            parts.push(address.state + ' ' + address.postalCode);
        else
            parts.push(address.state);
    }
    else if (address.county && options.county !== false) {
        if (address.postalCode && options.postalCode !== false)
            parts.push(address.county + ' ' + address.postalCode);
        else
            parts.push(address.county);
    }
    else if (address.postalCode && options.postalCode !== false)
        parts.push(address.postalCode);

    if (address.country && address.country != country)
        parts.push(address.country);

    let text = '';
    if (parts.length > 1) {
        text = parts[0];
        for (let i = 1, l = parts.length; i < l; i++)
            text += ', ' + parts[i];
    }
    else if (parts.length === 1)
        text = parts[0];

    return text;
}

declare global {
    interface Window {
        google: any;
    }
}

export enum CitySource {
    Locality = 1,
    PostalTown = 2,
    SubLocality = 3
}

import { Parse as parseAddress1 } from '../model/address1';

//https://maps.googleapis.com/maps/api/geocode/json?address=
export function Parse(gData: any) {
    var output: any = {};
    var address: any = {};

    if (gData.address_components) {
        var type, name, citySource = 0;
        for (var i = 0, l = gData.address_components.length; i < l; i++) {
            if (gData.address_components[i].types) {
                type = null;
                name = 'long_name';
                for (var j = 0; j < gData.address_components[i].types.length; j++) {
                    switch (gData.address_components[i].types[j]) {
                        case 'country':
                            type = 'country';
                            break;
                        case 'administrative_area_level_1':
                            type = 'state';
                            break;
                        case 'administrative_area_level_2':
                            type = 'county';
                            break;
                        case 'postal_town':
                            if (address.city)
                                address.address1 = address.city
                            type = 'city';
                            citySource = CitySource.PostalTown;
                            break;
                        case 'locality':
                            if (!address.city || citySource == CitySource.SubLocality) {
                                if (!address.address1 && address.city)
                                    address.address1 = address.city
                                type = 'city';
                                citySource = CitySource.Locality;
                            }
                            else
                                type = 'address1';
                            break;
                        case 'sublocality_level_1':
                        case 'sublocality':
                            if (!address.city) {
                                type = 'city';
                                citySource = CitySource.SubLocality;
                            }
                            else
                                type = 'address1';
                            break;
                        case 'administrative_area_level_3':
                        case 'neighborhood':
                            type = 'area';
                            break;
                        case 'street_number':
                            type = 'streetNumber';
                            break;
                        case 'route':
                            type = 'streetName';
                            name = 'short_name';
                            break;
                        case 'postal_code':
                            type = 'postalCode';
                            break;
                        default:
                            type = null;
                            break;
                    }
                    if (type)
                        address[type] = gData.address_components[i][name];
                }
            }
        }
        if (address.address1 && address.address1 == address.city)
            delete address.address1;
        if (Object.keys(address).length > 0)
            output.address = address;
    }

    if (gData.formatted_address)
        output.text = gData.formatted_address;

    if (gData.geometry) {
        output.geoLocation = { lat: gData.geometry.location.lat(), lng: gData.geometry.location.lng() };
        output.geometry = gData.geometry;
    }

    return output;
}

const numbers = "0123456789";

//https://developers.google.com/maps/documentation/geocoding/
export function geocode(textLocation: string | Geocoder.Geolocation, callback: Function, faultCallback?: Function) {
    var request: any = {};
    var dashIdx: number = -1, address1: string | undefined;
    if (typeof textLocation === 'string') {
        dashIdx = textLocation.replace(/–/g, '-').indexOf("-");
        //Look for xxx-yyy Street name
        var oneThird = textLocation.length / 3;
        if (dashIdx > 0 && dashIdx < oneThird) {
            for (var i = 0; i < dashIdx; i++) {
                if (!(textLocation[i].toLowerCase() !== textLocation[i].toUpperCase() || numbers.indexOf(textLocation[i]) !== -1 || textLocation[i] == '#' || textLocation[i] == ' ')) {
                    dashIdx = -1;
                    break;
                }
            }
            if (dashIdx >= 0 && numbers.indexOf(textLocation.substring(dashIdx + 1).trim()[0]) >= 0) {
                address1 = textLocation.substr(0, dashIdx).trim();
                if (address1.indexOf('#') >= 0) {
                    address1 = address1.replace(/#/g, '');
                    textLocation = textLocation.replace(/#/g, '');
                }
            }
        }

        if (!address1) {
            dashIdx = -1;

            var address = {
                value: textLocation
            };

            address1 = parseAddress1(address);

            if (address1)
                textLocation = address.value;
        }

        request.address = textLocation;
    }
    else {
        // @ts-ignore
        request.location = new google.maps.LatLng(textLocation.lat, textLocation.lng);
    }

    if (request.address || request.location) {
        // @ts-ignore
        var geocoder = new google.maps.Geocoder();

        geocoder.geocode(request, (results: any, status: any) => {
            // @ts-ignore
            if (status == google.maps.GeocoderStatus.OK) {
                var geocoded = Parse(results[0]);

                if (address1 && geocoded && geocoded.address) {
                    if (dashIdx > 0 && (geocoded.text.indexOf(address1) === -1 || address1.length == 1)) {
                        var letters = 2;
                        for (var i = 0; i < dashIdx; i++) {
                            if (address1[i].toLowerCase() !== address1[i].toUpperCase())
                                letters--;
                            if (letters < 0)
                                break;
                        }
                        if (letters >= 0)
                            geocoded.address.address1 = "Unit " + address1.toUpperCase();
                        else
                            geocoded.address.address1 = address1;
                    }
                    else if (dashIdx == -1)
                        geocoded.address.address1 = address1;
                }

                callback(geocoded);
            }
            else if (faultCallback) {
                faultCallback(status);
            }
        });
    }
}
