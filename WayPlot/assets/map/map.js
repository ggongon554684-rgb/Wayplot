(() => {
  "use strict";

  const map = L.map("map", { zoomControl: true, attributionControl: true })
    .setView([12.8797, 121.774], 5);
  const mapContainer = map.getContainer();
  mapContainer.setAttribute("tabindex", "0");

  const offlineNote = document.getElementById("offline-note");
  const mapNote = document.getElementById("map-note");
  const mapMessage = document.getElementById("map-message");
  const parcelLayers = new Map();
  const parcels = L.geoJSON(null, {
    style: feature => parcelStyle(feature.properties.status, false),
    pointToLayer: (feature, latlng) => L.circleMarker(latlng, {
      radius: 8,
      ...parcelStyle(feature.properties.status, false)
    }),
    onEachFeature: (feature, layer) => {
      const properties = feature.properties || {};
      const parcelId = String(properties.id || "");
      const label = document.createElement("span");
      label.textContent = String(properties.label || "");
      layer.bindTooltip(label, {
        permanent: true,
        direction: feature.geometry.type === "Point" ? "top" : "center",
        offset: feature.geometry.type === "Point" ? [0, -6] : [0, 0],
        className: "parcel-label",
        interactive: false
      });
      if (parcelId) {
        parcelLayers.set(parcelId, layer);
        layer.on("click", event => {
          if (activeMode) {
            return;
          }
          // stop() prevents the native event from bubbling AND stops Leaflet from
          // also firing its own map-level "click" (which would trigger mapBackgroundClicked
          // and immediately clear the inspector we are about to open).
          L.DomEvent.stop(event);
          selectParcel(parcelId);
          window.chrome.webview.postMessage({ type: "parcelClicked", parcelId });
        });
      }
    }
  }).addTo(map);

  const previewLayer = L.layerGroup().addTo(map);
  const vertexLayer = L.layerGroup().addTo(map);
  const editMarkerLayer = L.layerGroup().addTo(map);
  let selectedParcelId = null;
  let tileLayer = null;
  let activeMode = null;
  let modeKind = null;
  let modeParcelId = null;
  let modePoints = [];
  let doubleClickZoomWasEnabled = false;

  function parcelColor(status) {
    switch (status) {
      case "Available": return "#229664";
      case "Reserved": return "#e2961e";
      case "Sold": return "#cd4650";
      case "Leased": return "#6050c8";
      default: return "#808080";
    }
  }

  function parcelStyle(status, selected) {
    const color = parcelColor(status);
    return {
      color: selected ? "#14284b" : color,
      weight: selected ? 4 : 2,
      opacity: 1,
      fillColor: color,
      fillOpacity: selected ? 0.68 : 0.48
    };
  }

  function applySelection(layer, selected) {
    if (layer && typeof layer.setStyle === "function") {
      const properties = layer.feature && layer.feature.properties;
      layer.setStyle(parcelStyle(properties ? properties.status : "", selected));
      if (selected && typeof layer.bringToFront === "function") {
        layer.bringToFront();
      }
    }
  }

  function selectParcel(parcelId) {
    if (activeMode || selectedParcelId === parcelId) {
      return;
    }
    applySelection(parcelLayers.get(selectedParcelId), false);
    selectedParcelId = parcelId;
    applySelection(parcelLayers.get(selectedParcelId), true);
  }

  function fitAll() {
    const bounds = parcels.getBounds();
    if (bounds.isValid()) {
      map.fitBounds(bounds, { padding: [28, 28], maxZoom: 17 });
    } else {
      map.setView([12.8797, 121.774], 5);
    }
  }

  function fitParcel(parcelId) {
    const layer = parcelLayers.get(parcelId);
    if (!layer) {
      return;
    }
    if (typeof layer.getLatLng === "function") {
      map.setView(layer.getLatLng(), 18);
      return;
    }
    const bounds = typeof layer.getBounds === "function" ? layer.getBounds() : null;
    if (bounds && bounds.isValid()) {
      map.fitBounds(bounds, { padding: [40, 40], maxZoom: 19 });
    }
  }

  function focusParcel(parcelId) {
    requestAnimationFrame(() => {
      map.invalidateSize({ pan: false });
      requestAnimationFrame(() => fitParcel(parcelId));
    });
  }

  function loadParcels(featureCollectionJson) {
    let featureCollection;
    try {
      featureCollection = JSON.parse(featureCollectionJson);
    } catch {
      setMessage("Parcel map data was invalid and could not be displayed.");
      return;
    }

    parcelLayers.clear();
    parcels.clearLayers();
    selectedParcelId = null;
    if (featureCollection && featureCollection.type === "FeatureCollection"
      && Array.isArray(featureCollection.features)) {
      parcels.addData(featureCollection);
    }
    fitAll();
  }

  function configureTiles(tileUrlTemplate, attribution) {
    if (tileLayer) {
      map.removeLayer(tileLayer);
      tileLayer = null;
    }
    if (!tileUrlTemplate) {
      offlineNote.hidden = false;
      return;
    }

    try {
      tileLayer = L.tileLayer(tileUrlTemplate, {
        attribution: String(attribution || ""),
        maxZoom: 19,
        subdomains: "abc"
      });
      tileLayer.on("tileerror", () => { offlineNote.hidden = false; });
      tileLayer.on("tileload", () => { offlineNote.hidden = true; });
      tileLayer.addTo(map);
    } catch {
      offlineNote.hidden = false;
      tileLayer = null;
    }
  }

  function setNote(text) {
    mapNote.textContent = String(text || "");
    mapNote.hidden = !text;
  }

  function setMessage(text) {
    mapMessage.textContent = String(text || "");
    mapMessage.hidden = !text;
  }

  function pointsToLatLngs(points) {
    return points.map(point => [point.latitude, point.longitude]);
  }

  function renderGeometryPreview() {
    previewLayer.clearLayers();
    vertexLayer.clearLayers();
    if (activeMode === "polygon") {
      for (const point of modePoints) {
        L.circleMarker([point.latitude, point.longitude], {
          radius: 5,
          color: "#123d66",
          weight: 2,
          fillColor: "#ffffff",
          fillOpacity: 1
        }).addTo(vertexLayer);
      }
    }
    if (modePoints.length < 2) {
      return;
    }

    const latLngs = pointsToLatLngs(modePoints);
    L.polyline(latLngs, {
      color: "#175c9e",
      weight: 3,
      opacity: 0.9
    }).addTo(previewLayer);
    if (modeKind === "polygon" && modePoints.length >= 2) {
      L.polyline([latLngs[latLngs.length - 1], latLngs[0]], {
        color: "#175c9e",
        weight: 2,
        opacity: 0.35,
        dashArray: "5 6"
      }).addTo(previewLayer);
    }
  }

  function postModeState() {
    window.chrome.webview.postMessage({
      type: "modeState",
      mode: activeMode || "none",
      vertexCount: modePoints.length
    });
  }

  function stopMode() {
    if (!activeMode) {
      return;
    }
    map.off("click", handleModeClick);
    map.off("dblclick", finishMode);
    previewLayer.clearLayers();
    vertexLayer.clearLayers();
    editMarkerLayer.clearLayers();
    activeMode = null;
    modeKind = null;
    modeParcelId = null;
    modePoints = [];
    map.getPanes().overlayPane.style.pointerEvents = "";
    if (doubleClickZoomWasEnabled) {
      map.doubleClickZoom.enable();
    } else {
      map.doubleClickZoom.disable();
    }
    mapContainer.classList.remove("map-mode");
    postModeState();
  }

  function cancelMode() {
    if (!activeMode) {
      return;
    }
    stopMode();
    window.chrome.webview.postMessage({ type: "modeCancelled" });
  }

  function startMode(mode, kind, parcelId, points) {
    if (activeMode) {
      stopMode();
    }
    activeMode = mode;
    modeKind = kind;
    modeParcelId = parcelId;
    modePoints = points;
    doubleClickZoomWasEnabled = map.doubleClickZoom.enabled();
    map.doubleClickZoom.disable();
    map.getPanes().overlayPane.style.pointerEvents = "none";
    map.on("click", handleModeClick);
    map.on("dblclick", finishMode);
    mapContainer.classList.add("map-mode");
    mapContainer.focus();
    renderGeometryPreview();
    postModeState();
  }

  function addVertex(latlng) {
    if (modePoints.length >= 500) {
      postModeState();
      return;
    }
    if (modePoints.length > 0) {
      const previous = modePoints[modePoints.length - 1];
      const previousPoint = map.latLngToContainerPoint([previous.latitude, previous.longitude]);
      const nextPoint = map.latLngToContainerPoint(latlng);
      if (previousPoint.distanceTo(nextPoint) <= 1) {
        return;
      }
    }
    modePoints.push({ longitude: latlng.lng, latitude: latlng.lat });
    renderGeometryPreview();
    postModeState();
  }

  function handleModeClick(event) {
    if (activeMode === "polygon") {
      addVertex(event.latlng);
    }
  }

  function finishMode() {
    if (!activeMode) {
      return;
    }
    if (modeKind === "polygon" && modePoints.length < 3) {
      postModeState();
      return;
    }

    const wasEditing = activeMode === "editGeometry";
    const payload = {
      type: wasEditing ? "geometryEditComplete" : "drawComplete",
      kind: modeKind,
      points: modePoints.map(point => [point.longitude, point.latitude])
    };
    if (wasEditing) {
      payload.parcelId = modeParcelId;
    }
    stopMode();
    window.chrome.webview.postMessage(payload);
  }

  function undoVertex() {
    if (activeMode !== "polygon") {
      return;
    }
    modePoints.pop();
    renderGeometryPreview();
    postModeState();
  }

  function startDrawing() {
    setMessage(null);
    startMode("polygon", "polygon", null, []);
  }

  function geometryPoints(feature) {
    if (!feature || !feature.geometry) {
      return null;
    }
    const geometry = feature.geometry;
    if (geometry.type === "Point" && Array.isArray(geometry.coordinates)
      && geometry.coordinates.length === 2) {
      return {
        kind: "marker",
        points: [{ longitude: geometry.coordinates[0], latitude: geometry.coordinates[1] }]
      };
    }
    if (geometry.type !== "Polygon" || !Array.isArray(geometry.coordinates)
      || geometry.coordinates.length !== 1 || !Array.isArray(geometry.coordinates[0])) {
      return null;
    }
    const ring = geometry.coordinates[0].slice();
    if (ring.length > 1
      && Array.isArray(ring[0])
      && Array.isArray(ring[ring.length - 1])
      && ring[0][0] === ring[ring.length - 1][0]
      && ring[0][1] === ring[ring.length - 1][1]) {
      ring.pop();
    }
    if (ring.length < 3) {
      return null;
    }
    const points = ring.map(position => ({
      longitude: position[0],
      latitude: position[1]
    }));
    return { kind: "polygon", points };
  }

  function startGeometryEdit(parcelId) {
    const layer = parcelLayers.get(parcelId);
    const geometry = layer ? geometryPoints(layer.feature) : null;
    if (!geometry) {
      setMessage("This parcel boundary cannot be edited here. Redraw it as a new parcel instead.");
      window.chrome.webview.postMessage({ type: "modeCancelled" });
      return;
    }

    setMessage(null);
    startMode("editGeometry", geometry.kind, parcelId, geometry.points);
    geometry.points.forEach((point, index) => {
      const marker = L.marker([point.latitude, point.longitude], {
        draggable: true,
        keyboard: false
      }).addTo(editMarkerLayer);
      marker.on("drag", () => {
        const latlng = marker.getLatLng();
        modePoints[index] = { longitude: latlng.lng, latitude: latlng.lat };
        renderGeometryPreview();
      });
    });
    renderGeometryPreview();
  }

  function handleModeKey(event) {
    if (!activeMode) {
      return;
    }
    if (event.key === "Escape") {
      event.preventDefault();
      event.stopPropagation();
      cancelMode();
    } else if (event.key === "Enter") {
      event.preventDefault();
      event.stopPropagation();
      finishMode();
    } else if (event.key === "Backspace" && activeMode === "polygon") {
      event.preventDefault();
      event.stopPropagation();
      undoVertex();
    }
  }

  mapContainer.addEventListener("keydown", handleModeKey, true);
  map.on("click", () => {
    if (activeMode) {
      return;
    }
    selectParcel(null);
    window.chrome.webview.postMessage({ type: "mapBackgroundClicked" });
  });

  const resizeObserver = new ResizeObserver(() => map.invalidateSize({ pan: false }));
  resizeObserver.observe(document.getElementById("map"));

  window.chrome.webview.addEventListener("message", event => {
    const message = event.data;
    if (!message || typeof message.type !== "string") {
      return;
    }
    switch (message.type) {
      case "configureTiles":
        configureTiles(message.tileUrlTemplate, message.attribution);
        break;
      case "loadParcels":
        if (typeof message.featureCollectionJson === "string") {
          loadParcels(message.featureCollectionJson);
        }
        break;
      case "selectParcel":
        selectParcel(typeof message.parcelId === "string" ? message.parcelId : null);
        break;
      case "fitAll":
        fitAll();
        break;
      case "focusParcel":
        if (typeof message.parcelId === "string") {
          selectParcel(message.parcelId);
          focusParcel(message.parcelId);
        }
        break;
      case "note":
        setNote(message.text);
        break;
      case "message":
        setMessage(message.text);
        break;
      case "startDraw":
        startDrawing();
        break;
      case "startEditGeometry":
        if (typeof message.parcelId === "string") {
          startGeometryEdit(message.parcelId);
        }
        break;
      case "undoVertex":
        undoVertex();
        break;
      case "finishMode":
        finishMode();
        break;
      case "cancelMode":
        cancelMode();
        break;
    }
  });

  window.chrome.webview.postMessage({ type: "ready" });
})();
