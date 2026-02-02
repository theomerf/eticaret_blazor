(function () {
    let blazorLoaded = false;

    const checkBlazorLoaded = () => {
        const blazorContent = document.querySelector('#blazor-container [data-blazor-loaded]');

        if (blazorContent || blazorLoaded) {
            blazorLoaded = true;
            document.getElementById('pre-blazor-loading')?.classList.add('blazor-loaded');
            document.getElementById('blazor-container')?.classList.add('blazor-loaded');
            return true;
        }
        return false;
    };

    const observer = new MutationObserver(() => {
        if (checkBlazorLoaded()) {
            observer.disconnect();
        }
    });

    const container = document.getElementById('blazor-container');
    if (container) {
        observer.observe(container, {
            childList: true,
            subtree: true
        });
    }

    if (typeof Blazor !== 'undefined') {
        try {
            if (Blazor.start) {
                Blazor.start().then(() => {
                    setTimeout(() => {
                        if (!blazorLoaded) {
                            checkBlazorLoaded();
                            if (document.querySelector('#blazor-container [data-blazor-loaded]')) {
                                blazorLoaded = true;
                                document.getElementById('pre-blazor-loading')?.classList.add('blazor-loaded');
                                document.getElementById('blazor-container')?.classList.add('blazor-loaded');
                                observer.disconnect();
                            }
                        }
                    }, 500);
                }).catch(() => { });
            }
        } catch (e) { }
    } else {
        window.addEventListener('load', () => {
            setTimeout(() => {
                if (typeof Blazor !== 'undefined') {
                    try {
                        Blazor.start().then(() => {
                            setTimeout(() => {
                                if (!blazorLoaded) {
                                    checkBlazorLoaded();
                                }
                            }, 500);
                        }).catch(() => { });
                    } catch (e) { }
                }
            }, 100);
        });
    }

    setTimeout(checkBlazorLoaded, 100);
    const interval = setInterval(() => {
        if (checkBlazorLoaded()) {
            clearInterval(interval);
        }
    }, 200);

    setTimeout(() => {
        document.getElementById('pre-blazor-loading')?.classList.add('blazor-loaded');
        document.getElementById('blazor-container')?.classList.add('blazor-loaded');
    }, 5000);

})();
