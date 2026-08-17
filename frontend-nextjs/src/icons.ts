export async function registerIcons() {
    if (typeof window === 'undefined') return;
    const { registerIconLibrary } = await import('@awesome.me/webawesome');

    // Register the custom bizsrt legacy icon library
    registerIconLibrary('bizsrt', {
        resolver: name => `/images/bizsrt/${name}.svg`
    });

    // Register the custom flags legacy icon library
    registerIconLibrary('flags', {
        resolver: name => `/images/flags/${name}.svg`
    });
}
