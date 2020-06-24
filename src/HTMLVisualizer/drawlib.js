var BUS_CON_DIAMETER = 20;
var PADDING = 10;

var app = angular.module('SMEApp', []);
var ctrlScope = null;
var ctrl = app.controller('simCtrl', function($scope) {
    $scope.simcycle = 0;
    $scope.currentitem = null;
    $scope.setSimCycle = function(index) {
      $scope.simcycle = Math.max(0, Math.min($scope.values.length - 1, index));
    };

    $scope.setCurrentItem = function(item) {
      $scope.currentitem = item;
      if (item != null) {
        if (item.busid != null) {
          $scope.selectedType = 'Bus';
          $scope.selectedName = item.bus.name || item.bus.sourceclass;
          $scope.sourceClass = item.bus.name == null ? null : item.bus.sourceclass;

          $scope.signals = [].concat(item.bus.signals);


        } else if (item.processid != null) {
          $scope.selectedType = 'Process';
          $scope.selectedName = item.process.name || item.process.sourceclass;
          $scope.sourceClass = item.process.name == null ? null : item.process.sourceclass;

          $scope.signals = [].concat(item.process.variables);

        }

        var vmap = [];
        for (var i = $scope.signals.length - 1; i >= 0; i--) {
          var id = $scope.signals[i].id;
          for(var j = $scope.valuemap.length - 1; j >= 0; j--) {
            if ($scope.valuemap[j] == id) {
              vmap[i] = j;
              break;
            }
          }
        }

        $scope.signalmap = vmap;
      }
    }
    ctrlScope = $scope;
});

window.onload = function() {
  $.getJSON('./trace.json', null, function(data, status) {
    var ctrl = setupAngular(data);
    setupCanvas(data, ctrl);
  });
};

function setupAngular(data) {
  ctrlScope.$applyAsync(function(){
    ctrlScope.values = data.values;
    ctrlScope.valuemap = data.config.valuemap;
  });
};

function createBusEnd(data) {
  var con = new fabric.Ellipse({
    rx: BUS_CON_DIAMETER / 2,
    ry: BUS_CON_DIAMETER / 2,
    fill: '#afa',
  });

  con.busid = data;

  return con;
}

function createProcess(data) {

  var label = new fabric.Text(data.name || data.sourceclass, {
    left: PADDING * 2,
    top: 30,
    fontSize: 14
  });

  var core = new fabric.Rect({
    left: BUS_CON_DIAMETER,
    top: BUS_CON_DIAMETER,
    width: Math.max(30, label.width) + PADDING + BUS_CON_DIAMETER,
    height: 50,
    fill: '#faa'
  });

  var inbusses = [];
  var outbusses = []

  core.height = Math.max(core.height, (Math.max(data.inbusses.length, data.outbusses.length) * (BUS_CON_DIAMETER + PADDING)) + PADDING);
  label.top = ((core.height - label.height) / 2) + core.top;
  label.left = ((core.width - label.width) / 2) + core.left;

  for (var i = data.inbusses.length - 1; i >= 0; i--) {
    var b = inbusses[i] = createBusEnd(data.inbusses[i]);
    var area = core.height / data.inbusses.length;
    b.top = PADDING + (i * area) + (0.5 * area);
    b.left = BUS_CON_DIAMETER / 2;
  }

  for (var i = data.outbusses.length - 1; i >= 0; i--) {
      var b = outbusses[i] = createBusEnd(data.outbusses[i]);
      var area = core.height / data.outbusses.length;
      b.top = PADDING + (i * area) + (0.5 * area);
      b.left = core.width + BUS_CON_DIAMETER / 2;
  }

  var elements = [core, label].concat(inbusses, outbusses);
  var pg = new fabric.Group(elements, {
    left: 100,
    top: 100,
    width: core.width + BUS_CON_DIAMETER,
    height: core.height + BUS_CON_DIAMETER,
    centeredScaling: true,
    centeredRotation: true,
    hasControls: false
  });

  pg.process = data;
  pg.processid = data.id;
  pg.core = core;
  pg.label = label;

  pg.inbusses = inbusses;
  pg.outbusses = outbusses;

  for (var i = pg.inbusses.length - 1; i >= 0; i--) {
    pg.inbusses[i].pg_parent = pg;
  }
  for (var i = pg.outbusses.length - 1; i >= 0; i--) {
    pg.outbusses[i].pg_parent = pg;
  }

  return pg;
}

function createBus(data) {

  var label = new fabric.Text(data.name || data.sourceclass, {
    left: PADDING * 2,
    top: 30,
    fontSize: 14
  });

  var core = new fabric.Rect({
    left: BUS_CON_DIAMETER,
    top: BUS_CON_DIAMETER,
    width: Math.max(30, label.width) + PADDING + BUS_CON_DIAMETER,
    height: 50,
    fill: '#ffa'
  });
  label.top = ((core.height - label.height) / 2) + core.top;
  label.left = ((core.width - label.width) / 2) + core.left;

  var bin = createBusEnd(null);
  var area = core.height;
  bin.top = PADDING + (0.5 * area);
  bin.left = BUS_CON_DIAMETER / 2;

  var bout = createBusEnd(null);
  bout.top = PADDING + (0.5 * area);
  bout.left = core.width + BUS_CON_DIAMETER / 2;

  var elements = [core, label, bin, bout];
  var pg = new fabric.Group(elements, {
    left: 100,
    top: 100,
    width: core.width + BUS_CON_DIAMETER,
    height: core.height + BUS_CON_DIAMETER,
    centeredScaling: true,
    centeredRotation: true,
    hasControls: false
  });

  pg.bus = data;
  pg.busid = data.id;
  pg.core = core;
  pg.label = label;
  pg.inbus = bin;
  pg.outbus = bout;

  pg.inbus.pg_parent = pg;
  pg.outbus.pg_parent = pg;

  return pg;
}

function createBusConnection(output, input) {
  //var imout = fabric.util.invertTransform(output.transformMatrix);
  var brect = output.getBoundingRect(true, true);

  var ln = new fabric.Line([
    output.pg_parent.left + output.originalLeft,
    output.pg_parent.top + output.originalTop,
    input.pg_parent.left + input.originalLeft,
    input.pg_parent.top + input.originalTop
  ], {
    fill: 'red',
    stroke: 'red',
    strokeWidth: 2,
    selectable: false
  });

  ln.conn1 = output;
  ln.conn2 = input;

  if (output.lineends == null)
    output.lineends = [];
  output.lineends.push(ln);

  if (input.lineends == null)
    input.lineends = [];
  input.lineends.push(ln);

  return ln;
}

function setupCanvas(data) {
var Direction = {
  LEFT: 0,
  UP: 1,
  RIGHT: 2,
  DOWN: 3
};

var zoomLevel = 0;
var zoomLevelMin = 0;
var zoomLevelMax = 3;

var shiftKeyDown = false;
var mouseDownPoint = null;

var canvas = new fabric.Canvas('canvas', {
  width: 1024,
  height: 300,
  selectionKey: 'ctrlKey'
});

var idmap = {};

for (var i = data.config.processes.length - 1; i >= 0; i--) {
  idmap[data.config.processes[i].id] = createProcess(data.config.processes[i]);
}

for (var i = data.config.busses.length - 1; i >= 0; i--) {
  if (data.config.busses[i].isinternal)
    data.config.busses.splice(i, 1);
  else
    idmap[data.config.busses[i].id] = createBus(data.config.busses[i]);
}

// Layout algorithm here, takes the roots and then builds "layers" of processes
var remain_processes = [].concat(data.config.tree);
var remain_busses = [];
var layers = [];
var complete = [];
var currentlayer = [];

for (var i = data.config.busses.length - 1; i >= 0; i--) {
  remain_busses.push(data.config.busses[i].id);
}

// Extract as many processes as possible for each layer
while(remain_processes.length > 0) {
  for (var i = 0 ; i < remain_processes.length; i++) {
    var allcomplete = true;

    for (var j = remain_processes[i].parents.length - 1; j >= 0; j--) {
      allcomplete &= complete.indexOf(remain_processes[i].parents[j]) >= 0;
    }

    if (allcomplete) {
      currentlayer.push(remain_processes[i].self);
      remain_processes.splice(i, 1);
      i--;
    }
  }

  // Safeguard; make sure we always progress
  // rather than hang the browser
  if (currentlayer.length == 0) {
    currentlayer.push(remain_processes[i].self);
    remain_processes.splice(0, 1);
  }

  layers.push(currentlayer);
  complete = complete.concat(currentlayer);
  currentlayer = [];

  // Then add all the busses that the layer
  // writes to, in a new layer
  for (var i = remain_busses.length - 1; i >= 0; i--) {
    var removed = false;

    for(var j = layers[layers.length - 1].length - 1; j >= 0; j--) {
      var p = idmap[layers[layers.length - 1][j]];

      for(var k = p.outbusses.length - 1; k >= 0; k--) {
        if (p.outbusses[k].busid == remain_busses[i]) {
          currentlayer.push(remain_busses[i]);
          remain_busses.splice(i, 1);
          removed = true;
          break;
        }
      }
      if (removed)
        break;
    }
  }

  // If we added busses, make a new layer now
  if (currentlayer.length > 0) {
    layers.push(currentlayer);
    currentlayer = [];
  }

}

// Move items with no inputs to the last layer
for (var i = layers[0].length - 1; i >= 0; i--) {
  var el = idmap[layers[0][i]];

  if (el.outbusses && el.outbusses.length == 0) {
    layers[layers.length - 1].push(layers[0][i]);
    layers[0].splice(i, 1);
  }
}

// We now have the layers, find the "widest" layer
var maxitems = 1;
var maxheight = 0;
for (var i = layers.length - 1; i >= 0; i--) {
  maxitems = Math.max(layers[i].length, maxitems);
  for (var j = layers[i].length - 1; j >= 0; j--) {
    maxheight = Math.max(idmap[layers[i][j]].height, maxheight);
  }
}

// Position each layer
var offsetleft = 0;
for (var i = 0; i < layers.length; i++) {
  var maxwidth = 0;
  for (var j = layers[i].length - 1; j >= 0; j--) {
    el = idmap[layers[i][j]];
    if (el != null) {
      el.top = j * (maxheight + 50);
      el.left = offsetleft;
      maxwidth = Math.max(el.width, maxwidth);
    }
  }
  offsetleft += maxwidth + 100;
}


// Add the lines, connecting the busses
for(var k in idmap) {
  var el = idmap[k];
  if (el.process != null) {
    for (var i = el.inbusses.length - 1; i >= 0; i--) {
      var con = createBusConnection(el.inbusses[i], idmap[el.inbusses[i].busid].outbus);
      canvas.add(con);
    }
    for (var i = el.process.outbusses.length - 1; i >= 0; i--) {
      var con = createBusConnection(el.outbusses[i], idmap[el.outbusses[i].busid].inbus);
      canvas.add(con);
    }
  }
}

for (var i = data.config.processes.length - 1; i >= 0; i--) {
  canvas.add(idmap[data.config.processes[i].id]);
}

for (var i = data.config.busses.length - 1; i >= 0; i--) {
  canvas.add(idmap[data.config.busses[i].id]);
}

var canvasWidth = 0;
var canvasHeight = 0;
for (var i = layers.length - 1; i >= 0; i--) {
  for (var j = layers[i].length - 1; j >= 0; j--) {
    canvasHeight = Math.max(idmap[layers[i][j]].height + idmap[layers[i][j]].top, canvasHeight);
    canvasWidth = Math.max(idmap[layers[i][j]].width + idmap[layers[i][j]].left, canvasWidth);
  }
}

// Add a little padding
canvasWidth += 50;
canvasHeight += 50;

var zoom = Math.max(0.5, Math.min(canvas.width / canvasWidth, canvas.height / canvasHeight));
canvas.zoomToPoint({x: 0, y: 0}, zoom);
canvas.absolutePan(new fabric.Point(((canvasWidth * zoom) - canvas.width) / 2, ((canvasHeight * zoom) - canvas.height) / 2 ));
canvas.relativePan(new fabric.Point(25 * zoom, 25 * zoom));

// canvas.add(new fabric.Rect({
//   left: 300,
//   top: 300,
//   width: 50,
//   height: 50,
//   fill: '#afa'
// }));

canvas.on('mouse:down', function(opt) {
  var evt = opt.e;
  if (evt.altKey === true) {
    this.isDragging = true;
    this.selection = false;
    this.lastPosX = evt.clientX;
    this.lastPosY = evt.clientY;
  }
});
canvas.on('mouse:move', function(opt) {
  if (this.isDragging) {
    var e = opt.e;
    canvas.relativePan(new fabric.Point(e.clientX - this.lastPosX, e.clientY - this.lastPosY));
    this.lastPosX = e.clientX;
    this.lastPosY = e.clientY;
  }
});
canvas.on('mouse:up', function(opt) {
  this.isDragging = false;
  this.selection = true;
});

canvas.on('mouse:wheel', function(opt) {
  var delta = opt.e.deltaY;
  var pointer = canvas.getPointer(opt.e);
  var zoom = canvas.getZoom();
  zoom = zoom + delta/200;
  if (zoom > 20) zoom = 20;
  if (zoom < 0.5) zoom = 0.5;
  canvas.zoomToPoint({ x: opt.e.offsetX, y: opt.e.offsetY }, zoom);
  opt.e.preventDefault();
  opt.e.stopPropagation();
});

canvas.on('object:selected', function(e) {
  var p = e.target;
  ctrlScope.$applyAsync(function(){
    ctrlScope.setCurrentItem(p);
  });
});

canvas.on('object:moving', function(e) {
  var p = e.target;
  var lines = [];

  if (p.process != null) {
    for (var i = p.inbusses.length - 1; i >= 0; i--) {
      lines = lines.concat(p.inbusses[i].lineends);
    }
    for (var i = p.outbusses.length - 1; i >= 0; i--) {
      lines = lines.concat(p.outbusses[i].lineends);
    }
  } else if (p.bus != null) {
      lines = lines.concat(p.inbus.lineends || [], p.outbus.lineends || []);
  }

  for (var i = lines.length - 1; i >= 0; i--) {
    if (lines[i].conn1.pg_parent == p) {
      lines[i].set({'x1': p.left + lines[i].conn1.originalLeft, 'y1': p.top + lines[i].conn1.originalTop});
    } else if (lines[i].conn2.pg_parent == p) {
      lines[i].set({'x2': p.left + lines[i].conn2.originalLeft, 'y2': p.top + lines[i].conn2.originalTop});
    }
  }

  canvas.renderAll();
});

fabric.util.addListener(document.body, 'keydown', function(options) {
  var key = options.which || options.keyCode; // key detection
  if (key == 16) { // handle Shift key
    canvas.defaultCursor = 'move';
    canvas.selection = false;
    shiftKeyDown = true;
  } else if (key === 37) { // handle Left key
    move(Direction.LEFT);
  } else if (key === 38) { // handle Up key
    move(Direction.UP);
  } else if (key === 39) { // handle Right key
    move(Direction.RIGHT);
  } else if (key === 40) { // handle Down key
    move(Direction.DOWN);
  }
});
fabric.util.addListener(document.body, 'keyup', function(options) {
  var key = options.which || options.keyCode; // key detection
  if (key == 16) { // handle Shift key
    canvas.defaultCursor = 'default';
    canvas.selection = true;
    shiftKeyDown = false;
  }
});
jQuery('.canvas-container').on('mousewheel', function(options) {
  var delta = options.originalEvent.wheelDelta;
  if (delta != 0) {
    var pointer = canvas.getPointer(options.e, true);
    var point = new fabric.Point(pointer.x, pointer.y);
    if (delta > 0) {
      zoomIn(point);
    } else if (delta < 0) {
      zoomOut(point);
    }
  }
});

function move(direction) {
  switch (direction) {
    case Direction.LEFT:
      canvas.relativePan(new fabric.Point(-10 * canvas.getZoom(), 0));
      break;
    case Direction.UP:
      canvas.relativePan(new fabric.Point(0, -10 * canvas.getZoom()));
      break;
    case Direction.RIGHT:
      canvas.relativePan(new fabric.Point(10 * canvas.getZoom(), 0));
      break;
    case Direction.DOWN:
      canvas.relativePan(new fabric.Point(0, 10 * canvas.getZoom()));
      break;
  }
  //keepPositionInBounds(canvas);
}


function zoomIn(point) {
  if (zoomLevel < zoomLevelMax) {
    zoomLevel++;
    canvas.zoomToPoint(point, Math.pow(2, zoomLevel));
    keepPositionInBounds(canvas);
  }
}

function zoomOut(point) {
  if (zoomLevel > zoomLevelMin) {
    zoomLevel--;
    canvas.zoomToPoint(point, Math.pow(2, zoomLevel));
    keepPositionInBounds(canvas);
  }
}

function keepPositionInBounds() {
  var zoom = canvas.getZoom();
  var xMin = (2 - zoom) * canvas.getWidth() / 2;
  var xMax = zoom * canvas.getWidth() / 2;
  var yMin = (2 - zoom) * canvas.getHeight() / 2;
  var yMax = zoom * canvas.getHeight() / 2;

  var point = new fabric.Point(canvas.getWidth() / 2, canvas.getHeight() / 2);
  var center = fabric.util.transformPoint(point, canvas.viewportTransform);

  var clampedCenterX = clamp(center.x, xMin, xMax);
  var clampedCenterY = clamp(center.y, yMin, yMax);

  var diffX = clampedCenterX - center.x;
  var diffY = clampedCenterY - center.y;

  if (diffX != 0 || diffY != 0) {
    canvas.relativePan(new fabric.Point(diffX, diffY));
  }
}

function clamp(value, min, max) {
  return Math.max(min, Math.min(value, max));
}
};
