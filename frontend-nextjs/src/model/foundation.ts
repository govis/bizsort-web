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

        equalsTo(address: Address): boolean {
            if (!address ||
                this.country != address.country ||
                //Geocoder does not seem to populate State for UK address
                (this.state != address.state && this.state && !this.county) ||
                this.county != address.county ||
                this.city != address.city ||
                this.streetName != address.streetName)
                return false;
            else
                return true;
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
        Index: number;
        Length: number;
    }

    export interface DirectorySliceInput extends SliceInput {
        Category: number;
        Location: number;
        Skip: number[];
    }

    export interface SliceOutput {
        Series: number[];
        Index: number;
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

    get county(): LocationRef {
        return this.get(LocationType.Country);
    }

    get city(): LocationRef {
        return this.get(LocationType.City);
    }

    get(locationType) {
        var location: ILocation = this;
        while (location) {
            if (location.type === locationType)
                break;
            else if (location.type > locationType)
                location = location.parent;
            else
                location = null;
        }
        return location;
    }
}

export namespace Semantic {
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