﻿import * as React from 'react'
import { Link } from 'react-router'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { Dic } from '../Globals'
import { FindOptions, QueryDescription, FilterOptionParsed, FilterRequest } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, getQueryKey } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks } from '../Lines/LineBase'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString, isLite, isEntity } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityBase, EntityBaseProps} from './EntityBase'

export interface AutocompleteConfig<T> {
    getItems: (subStr: string) => Promise<T[]>;
    renderItem: (item: T, subStr?: string) => React.ReactNode;
    renderList?: (typeahead: Typeahead) => React.ReactNode;
    getEntityFromItem: (item: T) => Lite<Entity> | ModifiableEntity;
    getItemFromEntity: (entity: Lite<Entity> | ModifiableEntity) => Promise<T>;
}

export class LiteAutocompleteConfig implements AutocompleteConfig<Lite<Entity>>{

    constructor(
        public getItems: (subStr: string) => Promise<Lite<Entity>[]>,
        public withCustomToString: boolean) {
    }

    renderItem(item: Lite<Entity>, subStr: string) {
        return Typeahead.highlightedText(item.toStr || "", subStr)
    }

    getEntityFromItem(item: Lite<Entity>) {
        return item;
    }

    getItemFromEntity(entity: Lite<Entity> | ModifiableEntity): Promise<Lite<Entity>> {

        var lite = this.convertToLite(entity);;

        if (!this.withCustomToString)
            return Promise.resolve(lite);

        if (lite.id == undefined)
            return Promise.resolve(lite);

        return this.getItems(lite.id!.toString()).then(lites => {

            const result = lites.filter(a => a.id == lite.id).firstOrNull();

            if (!result)
                throw new Error("Impossible to getInitialItem with the current implementation of getItems");

            return result;
        });
    }

    convertToLite(entity: Lite<Entity> | ModifiableEntity) {
        
        if (isLite(entity))
            return entity;

        if (isEntity(entity))
            return toLite(entity, entity.isNew);

        throw new Error("Impossible to convert to Lite");
    }
}

export class FindOptionsAutocompleteConfig implements AutocompleteConfig<Lite<Entity>>{

    constructor(
        public findOptions: FindOptions,
        public count: number,
        public withCustomToString: boolean) {
    }

    parsedFilters?: FilterOptionParsed[];

    getParsedFilters(): Promise<FilterOptionParsed[]> {
        if (this.parsedFilters)
            return Promise.resolve(this.parsedFilters);

        return Finder.getQueryDescription(this.findOptions.queryName)
            .then(qd => Finder.parseFilterOptions(this.findOptions.filterOptions || [], qd))
            .then(filters => this.parsedFilters = filters);
    }

    getItems = (subStr: string): Promise<Lite<Entity>[]> => {
        return this.getParsedFilters()
            .then(filters => Finder.API.findLiteLikeWithFilters({
                queryKey: getQueryKey(this.findOptions.queryName),
                filters: filters.map(f => ({ token: f.token!.fullKey, operation: f.operation, value: f.value }) as FilterRequest),
                count: this.count,
                subString: subStr
            }));
    }

    renderItem(item: Lite<Entity>, subStr: string) {
        return Typeahead.highlightedText(item.toStr || "", subStr)
    }

    getEntityFromItem(item: Lite<Entity>) {
        return item;
    }

    getItemFromEntity(entity: Lite<Entity> | ModifiableEntity): Promise<Lite<Entity>> {

        var lite = this.convertToLite(entity);;

        if (!this.withCustomToString)
            return Promise.resolve(lite);

        if (lite.id == undefined)
            return Promise.resolve(lite);

        return Finder.API.findLiteLikeWithFilters({
            queryKey: getQueryKey(this.findOptions.queryName),
            filters:  [{ token: "Entity.Id", operation: "EqualTo", value: lite.id }],
            count: 1,
            subString: ""
        }).then(lites => {

            const result = lites.filter(a => a.id == lite.id).firstOrNull();

            if (!result)
                throw new Error("Impossible to getInitialItem with the current implementation of getItems");

            return result;
        });
    }

    convertToLite(entity: Lite<Entity> | ModifiableEntity) {

        if (isLite(entity))
            return entity;

        if (isEntity(entity))
            return toLite(entity, entity.isNew);

        throw new Error("Impossible to convert to Lite");
    }
}




