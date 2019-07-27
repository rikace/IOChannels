// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

$(function() {
    var videoContainer = document.getElementById('video-sse');
    if (videoContainer) {
        var source = new EventSource('/video-stream');
        source.onmessage = function (event) {
            videoContainer.innerHTML = event.data;
        };
    }
});


$(function() {
    var videoContainer = document.getElementById('video-pipelines');
    if (videoContainer) {

        var connection =
            new signalR.HubConnectionBuilder()
                .withUrl("/video-hub")
                .configureLogging(signalR.LogLevel.Information)
                .build();

        connection.start().then(() => {
            try {
                connection.stream("GetVideoStream")
                    .subscribe({
                        next: (item) => {
                            videoContainer.innerHTML = item;
                        }
                    });
            }
            catch (e) {
                console.error(e.toString());
            }
        });

    }
});