function update_point(cords, pColor) {
    var point = rank.append("g")
        .attr("class", "line-point");

    point.selectAll('circle')
        .data(function (d) { return d.values })
        .enter().append('circle')
        .attr("cx", function (d) { return x(d.date) })
        .attr("cy", function (d) { return y(d.ranking) })
        .attr("r", 3.5)
        .style("fill", "white")
        .style("stroke", function (d) { return color(d.name); });
}