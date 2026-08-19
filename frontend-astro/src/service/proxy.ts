/**
 * Wraps a plain JSON object in a Proxy that dynamically delegates missing properties/methods
 * to the provided Class prototype. This allows using OOP methods and getters on plain JSON
 * responses without the performance overhead of deep recursive deserialization.
 */
export function hydrateProxy<T extends object>(
    obj: any, 
    ClassConstructor: { new(...args: any[]): T },
    childMappings?: Record<string, any>
): T {
    if (!obj || typeof obj !== 'object') return obj;

    return new Proxy(obj, {
        get(target, prop, receiver) {
            // 1. Check if the property needs nested proxy hydration
            if (childMappings && typeof prop === 'string' && childMappings[prop] && target[prop] != null) {
                // Cache the proxied child on the target so we don't recreate the proxy on every access
                const proxyKey = `__proxy_${prop}`;
                if (!target[proxyKey]) {
                    target[proxyKey] = hydrateProxy(target[prop], childMappings[prop]);
                }
                return target[proxyKey];
            }

            // 2. Return the actual property from the JSON object if it exists
            if (prop in target) {
                return target[prop];
            }

            // 3. Fallback to the Class Prototype
            if (ClassConstructor && ClassConstructor.prototype) {
                // Traverse the prototype chain in case of inheritance
                let proto = ClassConstructor.prototype;
                while (proto && proto !== Object.prototype) {
                    const descriptor = Object.getOwnPropertyDescriptor(proto, prop);
                    if (descriptor) {
                        // If it's a getter, execute it with `this` bound to the Proxy (receiver)
                        // so that internal method calls (e.g. this.get()) also route back through the Proxy
                        if (descriptor.get) {
                            return descriptor.get.call(receiver);
                        }
                        // If it's a method, return it bound to the Proxy (receiver)
                        if (typeof descriptor.value === 'function') {
                            return descriptor.value.bind(receiver);
                        }
                    }
                    proto = Object.getPrototypeOf(proto);
                }
            }

            return undefined;
        }
    }) as T;
}
