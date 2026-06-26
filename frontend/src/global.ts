export interface Action<T1=any, T2=any> {
    (arg1: T1, arg2?: T2): void;
}

type Function<T = any> = (...args: any[]) => T;
export type Mixin<T extends Function> = InstanceType<ReturnType<T>>;
export type Constructor<T = object> = new (...args: any[]) => T;

export interface IEventHandler<T> {
    (sender: any, e: T): void;
}

export interface IPropertyBag {
    [name: string]: any;
}

export class Event<T> {
    private _handlers: IEventHandler<T>[] = [];

    public subscribe(handler: IEventHandler<T>): void {
        this._handlers.push(handler);
    }

    public unsubscribe(handler: IEventHandler<T>): void {
        this._handlers = this._handlers.filter(h => h !== handler);
    }

    public trigger(sender: any, data: T): void {
        this._handlers.slice(0).forEach(h => h(sender, data));
    }
}
