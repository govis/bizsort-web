
import type WaToast from '@awesome.me/webawesome/dist/components/toast/toast.js';

export function showToast(message: string, variant: 'brand' | 'success' | 'warning' | 'danger' | 'neutral' = 'danger', icon: string = 'exclamation-triangle', duration: number = 5000) {
    if (typeof document === 'undefined') {
        console.warn('SSR context: suppressing toast ->', message);
        return;
    }
    
    const toastStack = document.querySelector('wa-toast#app-toast') as WaToast | null;
    if (toastStack) {
        toastStack.create(message, {
            variant,
            duration,
            icon: { name: icon, library: 'system' }
        });
    } else {
        console.warn('Toast stack not found, cannot display:', message);
    }
}

