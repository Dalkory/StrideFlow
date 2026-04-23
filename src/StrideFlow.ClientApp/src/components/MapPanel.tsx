import { useEffect, useRef } from 'react'
import L from 'leaflet'

import type {
  LiveSessionResponse,
  WalkingSessionDetailResponse,
} from '../lib/types'

const DEFAULT_CENTER: L.LatLngTuple = [55.751244, 37.618423]

interface MapPanelProps {
  currentSession: WalkingSessionDetailResponse | null
  liveSessions: LiveSessionResponse[]
  userAccent: string
}

export function MapPanel({
  currentSession,
  liveSessions,
  userAccent,
}: MapPanelProps) {
  const hostRef = useRef<HTMLDivElement | null>(null)
  const mapRef = useRef<L.Map | null>(null)
  const overlayRef = useRef<L.LayerGroup | null>(null)

  useEffect(() => {
    if (!hostRef.current || mapRef.current) {
      return
    }

    const map = L.map(hostRef.current, {
      center: DEFAULT_CENTER,
      zoom: 12,
      zoomControl: false,
      attributionControl: false,
    })

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
    }).addTo(map)

    L.control.zoom({ position: 'bottomright' }).addTo(map)

    mapRef.current = map
    overlayRef.current = L.layerGroup().addTo(map)

    window.setTimeout(() => {
      map.invalidateSize()
    }, 0)

    return () => {
      map.remove()
      mapRef.current = null
      overlayRef.current = null
    }
  }, [])

  useEffect(() => {
    const map = mapRef.current
    const overlay = overlayRef.current
    if (!map || !overlay) {
      return
    }

    overlay.clearLayers()

    const bounds: L.LatLngTuple[] = []
    const currentPoints = currentSession?.points ?? []
    const currentCoordinates = currentPoints.map<L.LatLngTuple>((point) => [
      point.latitude,
      point.longitude,
    ])

    if (currentCoordinates.length > 0) {
      L.polyline(currentCoordinates, {
        color: userAccent,
        weight: 6,
        opacity: 0.92,
      }).addTo(overlay)

      const start = currentCoordinates[0]
      const finish = currentCoordinates[currentCoordinates.length - 1]

      L.circleMarker(start, {
        radius: 6,
        color: '#ffffff',
        weight: 2,
        fillColor: userAccent,
        fillOpacity: 1,
      })
        .bindTooltip('Старт')
        .addTo(overlay)

      L.circleMarker(finish, {
        radius: 8,
        color: '#ffffff',
        weight: 2,
        fillColor: '#102418',
        fillOpacity: 1,
      })
        .bindTooltip(
          `${currentSession?.session.name ?? 'Текущая прогулка'} · ${currentSession?.session.total_steps.toLocaleString('ru-RU') ?? '0'} шагов`,
        )
        .addTo(overlay)

      bounds.push(...currentCoordinates)
    }

    for (const session of liveSessions) {
      const tailCoordinates = session.tail_points.map<L.LatLngTuple>((point) => [
        point.latitude,
        point.longitude,
      ])

      if (tailCoordinates.length > 1) {
        L.polyline(tailCoordinates, {
          color: session.accent_color,
          weight: 3,
          opacity: 0.45,
          dashArray: '10 10',
        }).addTo(overlay)

        bounds.push(...tailCoordinates)
      }

      const liveMarker = L.circleMarker([session.latitude, session.longitude], {
        radius: 9,
        color: '#ffffff',
        weight: 2,
        fillColor: session.accent_color,
        fillOpacity: 1,
      })

      liveMarker
        .bindTooltip(
          `${session.display_name} · ${session.total_steps.toLocaleString('ru-RU')} шагов`,
        )
        .addTo(overlay)

      bounds.push([session.latitude, session.longitude])
    }

    if (bounds.length > 0) {
      map.fitBounds(L.latLngBounds(bounds), {
        padding: [36, 36],
        maxZoom: 15,
      })
    } else {
      map.setView(DEFAULT_CENTER, 12)
    }

    window.setTimeout(() => {
      map.invalidateSize()
    }, 0)
  }, [currentSession, liveSessions, userAccent])

  const hasData = (currentSession?.points.length ?? 0) > 0 || liveSessions.length > 0

  return (
    <div className="map-panel">
      <div ref={hostRef} className="map-panel__canvas" />
      {!hasData ? (
        <div className="map-panel__empty">
          <span className="eyebrow">Live map</span>
          <strong>Карта оживает, когда появляется маршрут.</strong>
          <p>Запусти прогулку с GPS или demo mode, и маршрут сразу появится на карте.</p>
        </div>
      ) : null}
    </div>
  )
}
