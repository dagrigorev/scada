#!/bin/bash

# RapidSCADA Service Verification Script
# Checks if all 5 microservices + database are running

set -e

echo "========================================="
echo "RapidSCADA Service Verification"
echo "========================================="
echo ""

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to check service health
check_service() {
    local service_name=$1
    local port=$2
    local endpoint=$3
    
    echo -n "Checking $service_name (port $port)... "
    
    if curl -f -s "http://localhost:$port$endpoint" > /dev/null 2>&1; then
        echo -e "${GREEN}✓ HEALTHY${NC}"
        return 0
    else
        echo -e "${RED}✗ UNHEALTHY${NC}"
        return 1
    fi
}

# Check PostgreSQL
echo -n "Checking PostgreSQL (port 5432)... "
if pg_isready -h localhost -p 5432 -U scada > /dev/null 2>&1; then
    echo -e "${GREEN}✓ HEALTHY${NC}"
else
    echo -e "${RED}✗ UNHEALTHY${NC}"
fi

# Check Redis (optional)
echo -n "Checking Redis (port 6379)... "
if redis-cli -h localhost -p 6379 ping > /dev/null 2>&1; then
    echo -e "${GREEN}✓ HEALTHY${NC}"
else
    echo -e "${YELLOW}⚠ NOT RUNNING (optional)${NC}"
fi

echo ""
echo "Checking Microservices:"
echo "----------------------"

# Check all microservices
check_service "Identity Service" 5003 "/health"
check_service "WebAPI Service" 5001 "/health"
check_service "Realtime Service" 5005 "/health"
check_service "Communicator Service" 5007 "/health"
check_service "Archiver Service" 5009 "/health"

# Check Web UI
check_service "Web UI" 3000 "/"

echo ""
echo "========================================="
echo "Service Discovery Endpoints:"
echo "========================================="

# Check service discovery
check_service "Service Discovery - List" 5001 "/api/discovery/services"
check_service "Service Discovery - Health" 5001 "/api/discovery/health"
check_service "Service Discovery - Endpoints" 5001 "/api/discovery/endpoints"

echo ""
echo "========================================="
echo "Summary"
echo "========================================="
echo ""
echo "All critical services checked!"
echo "Access the Web UI at: http://localhost:3000"
echo "Access Swagger API at: http://localhost:5001/swagger"
echo "Access Service Discovery at: http://localhost:3000/system/discovery"
echo ""
