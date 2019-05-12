
function getWidth() {
    return Math.max(
        document.body.scrollWidth,
        document.documentElement.scrollWidth,
        document.body.offsetWidth,
        document.documentElement.offsetWidth,
        document.documentElement.clientWidth
    );
}

function getHeight() {
    return Math.max(
        document.body.scrollHeight,
        document.documentElement.scrollHeight,
        document.body.offsetHeight,
        document.documentElement.offsetHeight,
        document.documentElement.clientHeight
    );
}

var width = getWidth(),
    height = getHeight();

var svg = d3.select("body").append("svg")
    .attr("width", width )
    .attr("height", height );

var projection = d3.geo.equirectangular()
    .scale(250)
    .translate([width / 2, height / 2])


var path = d3.geo.path()
    .projection(projection);

var graticule = d3.geo.graticule();
var url = "https://gist.githubusercontent.com/abenrob/787723ca91772591b47e/raw/8a7f176072d508218e120773943b595c998991be/world-50m.json";
d3.json(url, function (error, world) {
    svg.append("g")
        .attr("class", "land")
        .selectAll("path")
        .data([topojson.object(world, world.objects.land)])
        .enter().append("path")
        .attr("d", path);
    svg.append("g")
        .attr("class", "boundary")
        .selectAll("boundary")
        .data([topojson.object(world, world.objects.countries)])
        .enter().append("path")
        .attr("d", path);
});