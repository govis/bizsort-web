import { ViewModel } from '../../viewmodel';
import type { Pager as PagerModel } from './view';

export namespace PagerData {
    export interface IButton {
        canMove?: boolean;
        href?: string;
    }

    export interface IPageButton {
        pageNumber: number;
        selected?: boolean;
        href?: string;
    }
}

export interface IPagerInitOptions {
    master?: PagerModel;
}

export class ListPagerViewModel extends ViewModel {
    _master!: PagerModel;
    get master(): PagerModel { return this._master; }

    _mumericButtonCount = 5;
    
    protected _pageCount = 0;
    get pageCount(): number { return this._pageCount; }
    set pageCount(val: number) {
        if (this._pageCount !== val) {
            this._pageCount = val;
            this.notifyView(['pageCount']);
        }
    }
    
    isTotalItemCountFixed = false;

    firstPage: PagerData.IButton = {};
    previousPage: PagerData.IButton = {};
    nextPage: PagerData.IButton = {};
    lastPage: PagerData.IButton = {};
    
    protected _pageButtons: PagerData.IPageButton[] = [];
    get pageButtons() { return this._pageButtons; }

    initialize(options: IPagerInitOptions) {
        if (options.master) {
            this._master = options.master;
            this._master.observeProperty((sender, prop) => {
                switch (prop) {
                    case 'totalItemCount':
                        this._updatePageButtons();
                        break;
                    case 'itemCount':
                    case 'pageSize':
                        this._updatePageCount();
                        this._updatePageButtons();
                        break;
                    case 'pageIndex':
                        this._updatePageButtonIndexes();
                        break;
                    case 'canChangePage':
                        this._enableDisableButtons();
                        break;
                }
            });
            this._updatePageCount();
            this._updatePageButtons();
            this._updatePageButtonIndexes();
            this._enableDisableButtons();
        }
    }

    protected _updatePageCount() {
        if (this._master.pageSize && this._master.itemCount) {
            this.pageCount = Math.max(1, Math.ceil(this._master.itemCount / this._master.pageSize));
        } else {
            this.pageCount = 0;
        }
    }

    protected get _pageButtonIndex(): number {
        return Math.round(Math.min(Math.max((this._master.pageIndex + 1) - (this._mumericButtonCount / 2), 1), Math.max(this.pageCount - this._mumericButtonCount + 1, 1)));
    }

    protected _updatePageButtons() {
        const num = Math.min(this._mumericButtonCount, this.pageCount);
        if (this._pageButtons) {
            let count = this._pageButtons.length;
            while (count < num) {
                this._pageButtons.push({ pageNumber: 0 });
                count++;
            }
            while (count > num) {
                this._pageButtons.splice(0, 1);
                count--;
            }
            this._updatePageButtonIndexes();
        }
    }

    protected _updatePageButtonIndexes() {
        if (this._pageButtons) {
            const buttonStartIndex = this._pageButtonIndex;
            let buttonIndex = buttonStartIndex;
            
            for (let i = 0; i < this._pageButtons.length; i++) {
                const button = this._pageButtons[i];
                if (this._master.pageIndex == (buttonIndex - 1)) {
                    button.selected = true;
                } else {
                    button.selected = false;
                }
                button.pageNumber = buttonIndex;
                button.href = undefined; 
                buttonIndex++;
            }
            
            if (this.previousPage.canMove && this._master.pageIndex > 0) {
                this.previousPage.href = undefined;
            } else {
                this.previousPage.href = undefined;
            }
            
            if (this.nextPage.canMove && (this._master.pageIndex + 1) < this._master.pageCount) {
                this.nextPage.href = undefined;
            } else {
                this.nextPage.href = undefined;
            }

            this.notifyView(['pageButtons']);
            this._enableDisableButtons();
        }
    }

    protected _enableDisableButtons() {
        const needPage = this._master.pageSize > 0;
        if (!needPage || !this._master.canChangePage) {
            this._setCannotChangePage(needPage);
        } else {
            this._setCanChangePage();
            this._updateCanMoveFirstAndPrevious();
            this._updateCanMoveNextAndLast();
        }
    }

    protected _setCannotChangePage(needPage?: boolean) {
        this.firstPage.canMove = false;
        this.previousPage.canMove = false;
        this.nextPage.canMove = false;
        this.lastPage.canMove = false;
        this.notifyView(['firstPage', 'previousPage', 'nextPage', 'lastPage']);
    }

    protected _setCanChangePage() { }

    protected _updateCanMoveFirstAndPrevious() {
        this.firstPage.canMove = this._master.pageIndex > 0;
        this.previousPage.canMove = this.firstPage.canMove;
        this.notifyView(['firstPage', 'previousPage']);
    }

    protected _updateCanMoveNextAndLast() {
        this.nextPage.canMove = (!this.isTotalItemCountFixed || (this._master.totalItemCount == -1)) || (this._master.pageIndex < (this._master.pageCount - 1));
        this.lastPage.canMove = this._master.totalItemCount != -1 && (this._master.pageIndex < (this._master.pageCount - 1));
        this.notifyView(['nextPage', 'lastPage']);
    }
}
