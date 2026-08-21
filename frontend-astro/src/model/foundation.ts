// @ts-nocheck
﻿export interface Autocomplete extends NodeIdName {
    path: string[];
    //hasChildren?: boolean;
}

export interface EntityId {
    id: number;
}

export interface IdName extends EntityId {
    name: string;
}

export interface ILocation extends LocationRef {
    type: LocationType;
    parent: ILocation;
}

export enum ImageType {
    Jpeg = 1,
    Png = 2,
    Gif = 3
}

export interface INodeType {
    nodeType: NodeType;
    locked?: boolean;
}

export namespace Geocoder {
    export class Address {
        country: string;
        state: string;
        county: string;
        city: string;
        area: string;
        streetNumber: string;
        streetName: string;
        postalCode: string;
        address1: string;

        constructor(object?: Address) {
            if (object)
                Object.assign(this, object);
        }

        static equalsTo(a1: Address | any, a2: Address | any): boolean {
            if (!a1 || !a2 ||
                a1.country != a2.country ||
                //Geocoder does not seem to populate State for UK address
                (a1.state != a2.state && a1.state && !a1.county) ||
                a1.county != a2.county ||
                a1.city != a2.city ||
                a1.streetName != a2.streetName)
                return false;
            else
                return true;
        }

        equalsTo(address: Address): boolean {
            return Address.equalsTo(this, address);
        }
    }

    export interface Geolocation {
        lat: number;
        lng: number;
    }

    export class Location {
        constructor(data?: Location) {
            if (data) {
                this.id = data.id;
                this.address = new Address(data.address);
                if (data.text)
                    this.text = data.text;
                if (data.geoLocation)
                    this.geoLocation = data.geoLocation;
            }
            else
                this.address = new Address();

        }
        id: number;
        address: Address;
        /*Do not pick up place_id from Geocoder, get it from Place search API
        PlaceId: string;*/
        text: string;
        geoLocation: Geolocation;
    }
}

export namespace List {
    export namespace Filter {
        export class QueryInput implements List.QueryInput {
            searchQuery: string;
            startIndex: number;
            length: number;
            inclFacets: Semantic.FacetFilter;
            exclFacets: Semantic.FacetFilter;

            constructor(facets: Semantic.Facet[]) {
                if (facets && facets.length > 0) {
                    facets = facets.slice(); //make a copy
                    var sorted = facets.sort((f1, f2) => {
                        return f1.name - f2.name;
                    });

                    this.inclFacets = this.facetFilter(sorted, false);
                    this.exclFacets = this.facetFilter(sorted, true);
                }
                else {
                    this.inclFacets = this.facetFilter();
                    this.exclFacets = this.facetFilter();
                }
            }

            facetFilter(facets?: Semantic.Facet[], excluded?): Semantic.FacetFilter {
                if (facets && facets.length > 0) {
                    var fiters = facets.filter(f => (f.exclude || false) == excluded);

                    if (fiters.length > 0) {
                        return {
                            noFilters: fiters.length,
                            filterNames: fiters.map(f => f.name),
                            filterValues: fiters.map(f => f.value)
                        };
                    }
                }
                return {
                    noFilters: 0
                }
            }
        }

        export interface QueryOutput extends List.QueryOutput {
            facets: Semantic.FacetName[];
        }
    }

    export interface QueryInput {
        searchQuery?: string;
        startIndex: number;
        length: number;
    }

    export interface QueryOutput {
        startIndex: number;
        series: EntityId[];
        totalCount?: number;
    }

    export interface SearchInput extends QueryInput {
        category: number;
        location: number;
        searchNear: Geocoder.Geolocation;
    }

    export interface SearchOutput extends QueryOutput {
        distances: number[];
    }

    export interface LocationQueryInput extends QueryInput {
        location: number;
    }

    export interface SliceInput {
        index: number;
        length: number;
    }

    export interface DirectorySliceInput extends SliceInput {
        category: number;
        location: number;
        skip?: number[];
    }
}

export interface LocationRef extends IdName {
    type: LocationType;
}

export enum LocationType {
    Unknown = 0,
    Country = 1,
    State = 2,
    County = 4,
    City = 8,
    Street = 16,
    Area = 32
}

export interface NodeIdName extends IdName {
    nodeType: NodeType;
}

export interface Node extends NodeIdName {
    parent?: Node;
    hasChildren?: boolean;
    children?: Node[];
    locked?: boolean;
}

export namespace Node {
    export interface DeserializeOptions {
        populate?: (NodeRef) => void;
        navToken?: any;
    }

    /*export function deserialize(node: Node, options: DeserializeOptions = {}, dic: Object = {}) {
        var parent = node.parent;
        if (parent && parent['$ref'])
            node.parent = dic[parent['$ref']];
        if (node.children) {
            dic[node['$id']] = node;
            for (var i = 0, l = node.children.length; i < l; i++) {
                deserialize(node.children[i], dic, options);
            }
        }
        if (options.populate)
            options.populate(node)
        else
            reflectLocked(node);
        if (options.navToken)
            node['navToken'] = options.navToken(node);
    }*/

    export function deserialize(node: Node, options: DeserializeOptions = {}, parent?: Node) {
        if (parent && !node.parent)
            node.parent = parent;
        if (node.children) {
            for (var i = 0, l = node.children.length; i < l; i++) {
                deserialize(node.children[i], options, node);
            }
        }
        if (options.populate)
            options.populate(node)
        else
            reflectLocked(node);
        if (options.navToken)
            node['navToken'] = options.navToken(node);
    }

    export function deserializeChildren(nodes: NodeRef[], parent: NodeRef, options: DeserializeOptions = {}) {
        for (var i = 0, l = nodes.length; i < l; i++) {
            if (parent)
                _setParent(nodes[i], parent.id, parent);
            if (nodes[i].hasChildren) {
                if (nodes[i].children) {
                    deserializeChildren(nodes[i].children, nodes[i], options);
                }
                else
                    nodes[i].children = [{ id: 0, name: "...", hasChildren: false }];
            }
            if (options.populate)
                options.populate(nodes[i]);
            else
                reflectLocked(<any>nodes[i]);
            if (options.navToken)
                nodes[i]['navToken'] = options.navToken(nodes[i]);
        }
    }

    export function setParent(nodes: NodeRef[], parent) {
        var parentId = parent ? parent.id : 0;
        for (var i = 0, l = nodes.length; i < l; i++) {
            _setParent(nodes[i], parentId, parent);
        }
    }

    function _setParent(node: NodeRef, parentId: number, parent: NodeRef) {
        if (!node.parentId)
            node.parentId = parentId;
        else if (node.parentId != parentId)
            throw 'Parent folder mismatch: ' + node.parentId + '!=' + parentId;
        if (parentId && parent && !node.parent) //It's important to keep .ContainerX props
            node.parent = parent;
    }

    export function isDefaultCategory(node: NodeRef) {
        return node && node.id == 0 ? true : false;
    }
}

export interface NodeRef extends IdName {
    parentId?: number;
    parent?: NodeRef;
    hasChildren?: boolean;
    children?: NodeRef[];
    locked?: boolean;
    //navToken?: any;
}

export enum NodeType {
    Super = 1,
    Class = 2
}

export function reflectLocked(group: INodeType) {
    if ((group.nodeType & NodeType.Class) == 0)
        group.locked = true;
}

export class ResolvedLocation implements ILocation {
    id: number;
    name: string;
    type: LocationType;
    parent: ILocation;
    partial: boolean;

    constructor(data?: ResolvedLocation) {
        if (data) {
            this.id = data.id;
            this.name = data.name;
            this.type = data.type;
            if (data.parent)
                this.parent = data.parent;
            if (data.partial)
                this.partial = data.partial;
        }
    }

    static getCounty(location: ILocation | any): LocationRef | null {
        return ResolvedLocation.getLocation(location, LocationType.County);
    }

    static getCity(location: ILocation | any): LocationRef | null {
        return ResolvedLocation.getLocation(location, LocationType.City);
    }

    static getLocation(location: ILocation | any, locationType: LocationType): LocationRef | null {
        var loc: ILocation = location;
        while (loc) {
            if (loc.type === locationType)
                break;
            else if (loc.type > locationType)
                loc = loc.parent;
            else
                loc = null as any;
        }
        return loc;
    }

    get county(): LocationRef | null {
        return ResolvedLocation.getCounty(this);
    }

    get city(): LocationRef | null {
        return ResolvedLocation.getCity(this);
    }

    get(locationType: LocationType): LocationRef | null {
        return ResolvedLocation.getLocation(this, locationType);
    }
}

export namespace Semantic {
    export class QueryInput {
        searchQuery?: string;
        startIndex: number;
        exclude?: boolean;
        inclFacets?: FacetFilter;
        exclFacets?: FacetFilter;

        constructor(facets?: Facet[], public length: number = 0) {
            this.startIndex = 0;
            if (facets && facets.length > 0) {
                facets = facets.slice();
                var sorted = facets.sort((f1, f2) => f1.name - f2.name);
                this.inclFacets = QueryInput.facetFilter(sorted, false);
                this.exclFacets = QueryInput.facetFilter(sorted, true);
            } else {
                this.inclFacets = QueryInput.facetFilter();
                this.exclFacets = QueryInput.facetFilter();
            }
        }

        static facetFilter(facets?: Facet[], excluded?: boolean): FacetFilter {
            if (facets && facets.length > 0) {
                var filters = facets.filter(f => (f.exclude || false) == excluded);
                if (filters.length > 0) {
                    return {
                        noFilters: filters.length,
                        filterNames: filters.map(f => f.name),
                        filterValues: filters.map(f => f.value)
                    };
                }
            }
            return { noFilters: 0 };
        }
        
        facetFilter(facets?: Facet[], excluded?: boolean): FacetFilter {
            return QueryInput.facetFilter(facets, excluded);
        }
    }

    export namespace Facet {
        export function deserialize(facets: FacetName[]) {
            if (facets && facets.length > 0)
                for (var i = 0, l = facets.length; i < l; i++) {
                    _deserialize(facets[i]);
                }
        }

        function _deserialize(facet: FacetName) {
            if (facet.values && facet.values.length > 0)
                for (var i = 0; i < facet.values.length; i++) {
                    facet.values[i].name = facet;
                }
        }
    }

    export interface Facet {
        name: number;
        nameText: string;
        value: number;
        valueText: string;
        exclude?: boolean;
    }

    export interface FacetFilter {
        noFilters: number;
        filterNames?: number[];
        filterValues?: number[];
    }

    export interface FacetName {
        key: number;
        text: string;
        values: FacetValue[];
    }

    export interface FacetValue {
        name: FacetName;
        key: number;
        text: string;
        count: number;
    }
}

export enum ServiceProvider {
    BizSrt = 1,
    Google = 2,
    Facebook = 3
}

export enum SubType {
    None = 0,
    Siblings = 1,
    Children = 2,
    GrandChildren = 4
}