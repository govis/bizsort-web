import { ViewModel } from '../../viewmodel';
import type { Action } from '../../global';

export namespace Semantic {
    export interface Facet {
        name: string;
        nameText: string;
        value: string;
        valueText: string;
        exclude?: boolean;
    }
    
    export interface FacetValue {
        name: { key: string, text: string };
        key: string;
        text: string;
        count: number;
    }
    
    export interface FacetName {
        key: string;
        text: string;
        values: FacetValue[];
    }
}

export class ListFilterAvailableViewModel extends ViewModel {
    filterSelected?: Action<Semantic.Facet>;

    protected _facets: Semantic.FacetName[] = [];
    get facets(): Semantic.FacetName[] {
        return this._facets;
    }

    populate(facets: Semantic.FacetName[]) {
        if (this._facets !== facets) {
            this._facets = facets;
            this.notifyView(['facets']);
        }
    }

    filterIn(facet: Semantic.FacetValue) {
        if (facet) {
            this.onFilterSelected({
                name: facet.name.key,
                nameText: facet.name.text,
                value: facet.key,
                valueText: facet.text,
                exclude: false
            });
        }
    }

    filterOut(facet: Semantic.FacetValue) {
        if (facet) {
            this.onFilterSelected({
                name: facet.name.key,
                nameText: facet.name.text,
                value: facet.key,
                valueText: facet.text,
                exclude: true
            });
        }
    }

    onFilterSelected(facet: Semantic.Facet) {
        if (this.filterSelected) {
            this.filterSelected(facet);
        }
    }
}

export class ListFilterAppliedViewModel extends ViewModel {
    filterSelected?: Action<Semantic.Facet>;

    protected _facets: Semantic.Facet[] = [];
    get facets(): Semantic.Facet[] {
        return this._facets;
    }

    clear() {
        if (this._facets.length) {
            this._facets.length = 0;
            this.notifyView(['facets']);
        }
    }

    add(facet: Semantic.Facet) {
        this._facets.push(facet);
        this.notifyView(['facets']);
    }

    remove(facet: Semantic.Facet, index?: number) {
        if (index === undefined) {
            index = facet ? this._facets.indexOf(facet) : -1;
        }
        if (index > -1) {
            this._facets.splice(index, 1);
            this.notifyView(['facets']);
        }
    }

    onFilterSelected(facet: Semantic.Facet) {
        if (this.filterSelected) {
            this.filterSelected(facet);
        }
    }
}
