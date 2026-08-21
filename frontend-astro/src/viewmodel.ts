import type { Action } from './global'
import { Event } from './global'
import { ErrorMessageType } from './exception'

export type { Action }
export { Event, ErrorMessageType }

export interface IViewAdapter {
    host?: any;
    modelUpdated(props: string[]): void;
}

export class ErrorInfo {
    protected _errors: { [name: string]: string } = {};
    public hasErrors = false;

    constructor(public validateable: Validateable) {}

    setError(name: string, error: string) {
        this.hasErrors = true;
        this._errors[name] = error;
        if (this.validateable && this.validateable.viewModel && this.validateable.viewModel.view) {
            this.validateable.viewModel.view.modelUpdated(['errorInfo']);
        }
    }

    getError(name: string) {
        return this._errors[name];
    }

    clear() {
        this._errors = {};
        this.hasErrors = false;
        if (this.validateable && this.validateable.viewModel && this.validateable.viewModel.view) {
            this.validateable.viewModel.view.modelUpdated(['errorInfo']);
        }
    }
}

export class Validateable {
    public errorInfo: ErrorInfo;
    
    constructor(
        public viewModel: ViewModel, 
        _options?: any, 
        _validator?: any, 
        public customValidate?: (proceed: (result: boolean) => void) => void
    ) {
        this.errorInfo = new ErrorInfo(this);
    }

    validate(): boolean {
        if (this.customValidate) {
            let isValid = false;
            this.customValidate((result) => { isValid = result; });
            return isValid;
        }
        return !this.errorInfo.hasErrors;
    }
}

export class ViewModel {
    protected _validateable?: Validateable;
    get validateable() { return this._validateable!; }

    constructor(public view: IViewAdapter) {}

    notifyView(props: string[]) {
        if (this.view && this.view.modelUpdated) {
            this.view.modelUpdated(props);
        }
    }
}