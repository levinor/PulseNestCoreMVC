
var connection = new signalR.HubConnectionBuilder()
    .withUrl("/Hubs/updaterHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();


connection.on("ReceivePoint", function(data) {
    console.log("Recibido punto");
    console.log(data);
    svg.selectAll("circle")
        .data(data.coordinates).enter()
        .append("circle")
        .attr("cx", function (d) { console.log(projection(d)); return projection(d)[0]; })
        .attr("cy", function (d) { return projection(d)[1]; })
        .attr("r", "8px")
        .attr("fill", data.color)
});

connection.onclose(() => setTimeout(startSignalRConnection(connection), 1000));

var startSignalRConnection = connection => connection.start()
    .then(() => console.info('Websocket Connection Established'))
    .catch(err => console.error('SignalR Connection Error: ', err));

connection.start()