import { gql, type TypedDocumentNode } from '@apollo/client';
import type {
  AirQualityData,
  EnergyStatsData,
  MotionData,
  QueryVars,
} from '../types/sensor';

export const AGGREGATE_AIR_QUALITY: TypedDocumentNode<AirQualityData, QueryVars> = gql`
  query AggregateAir($location: String!, $from: String!, $to: String!, $interval: String!) {
    aggregateAirQuality(location: $location, from: $from, to: $to, interval: $interval) {
      timeBucket
      avgCo2
      avgPm25
      avgHumidity
      maxCo2
    }
  }
`;

export const AGGREGATE_ENERGY: TypedDocumentNode<EnergyStatsData, QueryVars> = gql`
  query AggregateEnergy($location: String!, $from: String!, $to: String!, $interval: String!) {
    aggregateEnergy(location: $location, from: $from, to: $to, interval: $interval) {
      timeBucket
      totalEnergy
      avgPower
      peakPower
    }
  }
`;

export const AGGREGATE_MOTION: TypedDocumentNode<MotionData, QueryVars> = gql`
  query AggregateMotion($location: String!, $from: String!, $to: String!, $interval: String!) {
    aggregateMotion(location: $location, from: $from, to: $to, interval: $interval) {
      timeBucket
      eventCount
      isConstant
    }
  }
`;

export const GET_LATEST_READINGS = gql`
  query GetLatest($location: String) {
    getAirQualityHistory(location: $location, limit: 1) {
      items { co2 pm25 humidity timestamp }
    }
    getEnergyHistory(location: $location, limit: 1) {
      energy timestamp
    }
    getMotionHistory(location: $location, limit: 1) {
      motionDetected timestamp
    }
  }
`;