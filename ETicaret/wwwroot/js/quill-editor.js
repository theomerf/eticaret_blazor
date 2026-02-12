window.initializeQuillEditor = function (editorId, placeholder, initialValue, dotNetHelper) {
    const editorElement = document.getElementById(editorId);
    if (!editorElement) {
        console.error('Editor element not found:', editorId);
        return null;
    }

    const quill = new Quill('#' + editorId, {
        theme: 'snow',
        placeholder: placeholder,
        modules: {
            toolbar: {
                container: [
                    [{ 'header': [1, 2, 3, 4, 5, 6, false] }],
                    ['bold', 'italic', 'underline', 'strike'],
                    [{ 'list': 'ordered' }, { 'list': 'bullet' }],
                    [{ 'color': [] }, { 'background': [] }],
                    [{ 'align': [] }],
                    ['link', 'image'],
                    ['clean']
                ],
                handlers: {
                    image: imageHandler
                }
            }
        }
    });

    function imageHandler() {
        const url = prompt('Resim URL\'sini girin:');
        if (url) {
            const range = quill.getSelection();
            quill.insertEmbed(range.index, 'image', url);
        }
    }

    if (initialValue) {
        quill.root.innerHTML = initialValue;
    }

    quill.on('text-change', function () {
        const htmlContent = quill.root.innerHTML;
        dotNetHelper.invokeMethodAsync('OnContentChanged', htmlContent);
    });

    return {
        dispose: function () {
        }
    };
};