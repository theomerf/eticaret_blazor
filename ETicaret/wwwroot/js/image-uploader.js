// Initialize SortableJS for image drag-and-drop reordering
window.initializeImageSortable = function (containerId, dotNetHelper) {
    const container = document.getElementById(containerId);
    if (!container) {
        console.error('Sortable container not found:', containerId);
        return null;
    }

    const sortable = new Sortable(container, {
        animation: 200,
        handle: '.drag-handle',
        ghostClass: 'sortable-ghost',
        dragClass: 'sortable-drag',
        chosenClass: 'sortable-chosen',
        forceFallback: true,
        fallbackClass: 'sortable-fallback',
        fallbackOnBody: true,
        swapThreshold: 0.65,

        onEnd: function (evt) {
            // Get new order of image IDs
            const imageUrls = Array.from(container.querySelectorAll('.image-preview-card'))
                .map(card => card.getAttribute('data-image-id'));

            // Notify Blazor of the new order
            dotNetHelper.invokeMethodAsync('OnImagesReordered', imageUrls);
        }
    });

    return {
        dispose: function () {
            if (sortable) {
                sortable.destroy();
            }
        }
    };
};
