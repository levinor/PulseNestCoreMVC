"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/updaterHub").build();

connection.on("ReceivePoint", function (data, color) {
    var circles = svg.selectAll("circle")
        .data(data)
        .enter()
        .append("circle")
        .attr("cx", function (d) {
            return projection([d.longitude, d.latitude])[0];
        })
        .attr("cy", function (d) {
            return projection([d["longitude"], d["latitude"]])[1];
        })
        .attr("r", 2)
    Console.log("Recibido punto");

});

connection.start()