
var connection = new signalR.HubConnectionBuilder()
    .withUrl("/Hubs/updaterHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();



connection.on("ReceivePoint", function(data) {
    console.log("Recibido punto");
    console.log(data);
    var coordinates = projection(data.coordinates)
    svg.append("circle")
        .attr("cx", coordinates[0])
        .attr("cy", coordinates[1])
        .attr("r", "5px")
        .attr("fill", data.color)
        .attr("opacity", 1)
        .transition()
        .duration(3000)
        .attr("opacity", 0);
});

connection.onclose(() => setTimeout(startSignalRConnection(connection), 1000));

var startSignalRConnection = connection => connection.start()
    .then(() => console.info('Websocket Connection Established'))
    .catch(err => console.error('SignalR Connection Error: ', err));

connection.start();
