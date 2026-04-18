#!/bin/bash

# RapidSCADA API Endpoint Testing Script
# Tests all major API endpoints using cURL

set -e

echo "========================================="
echo "RapidSCADA API Endpoint Tests"
echo "========================================="
echo ""

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Base URLs
IDENTITY_URL="http://localhost:5003"
WEBAPI_URL="http://localhost:5001"
REALTIME_URL="http://localhost:5005"
COMMUNICATOR_URL="http://localhost:5007"
ARCHIVER_URL="http://localhost:5009"

# Test counter
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Function to test endpoint
test_endpoint() {
    local method=$1
    local url=$2
    local description=$3
    local expected_code=${4:-200}
    local data=$5
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    echo -n "[$TOTAL_TESTS] Testing $description... "
    
    if [ -n "$data" ]; then
        response=$(curl -s -w "\n%{http_code}" -X $method "$url" \
            -H "Content-Type: application/json" \
            -d "$data")
    else
        response=$(curl -s -w "\n%{http_code}" -X $method "$url")
    fi
    
    http_code=$(echo "$response" | tail -n1)
    
    if [ "$http_code" -eq "$expected_code" ]; then
        echo -e "${GREEN}✓ PASS${NC} (HTTP $http_code)"
        PASSED_TESTS=$((PASSED_TESTS + 1))
        return 0
    else
        echo -e "${RED}✗ FAIL${NC} (HTTP $http_code, expected $expected_code)"
        FAILED_TESTS=$((FAILED_TESTS + 1))
        return 1
    fi
}

echo "========================================="
echo "1. Identity Service Tests"
echo "========================================="

test_endpoint "GET" "$IDENTITY_URL/health" "Health check"

# Note: Login will fail without user, but we test the endpoint exists
test_endpoint "POST" "$IDENTITY_URL/api/auth/login" "Login endpoint" 400 \
    '{"username":"testuser","password":"testpass"}'

echo ""
echo "========================================="
echo "2. WebAPI Service Tests"
echo "========================================="

test_endpoint "GET" "$WEBAPI_URL/health" "Health check"
test_endpoint "GET" "$WEBAPI_URL/api/discovery/services" "Service Discovery - List all services"
test_endpoint "GET" "$WEBAPI_URL/api/discovery/health" "Service Discovery - Health status"
test_endpoint "GET" "$WEBAPI_URL/api/discovery/endpoints" "Service Discovery - All endpoints"
test_endpoint "GET" "$WEBAPI_URL/api/discovery/services/Identity" "Service Discovery - Get Identity service"

# These will return 401 Unauthorized without token, which is expected
test_endpoint "GET" "$WEBAPI_URL/api/devices" "Get all devices" 401
test_endpoint "GET" "$WEBAPI_URL/api/tags" "Get all tags" 401
test_endpoint "GET" "$WEBAPI_URL/api/alarms" "Get all alarms" 401

echo ""
echo "========================================="
echo "3. Realtime Service Tests"
echo "========================================="

test_endpoint "GET" "$REALTIME_URL/health" "Health check"
test_endpoint "GET" "$REALTIME_URL/metrics" "Metrics endpoint"

echo ""
echo "========================================="
echo "4. Communicator Service Tests"
echo "========================================="

test_endpoint "GET" "$COMMUNICATOR_URL/health" "Health check"

echo ""
echo "========================================="
echo "5. Archiver Service Tests"
echo "========================================="

test_endpoint "GET" "$ARCHIVER_URL/health" "Health check"

echo ""
echo "========================================="
echo "Test Summary"
echo "========================================="
echo ""
echo "Total Tests: $TOTAL_TESTS"
echo -e "${GREEN}Passed: $PASSED_TESTS${NC}"
echo -e "${RED}Failed: $FAILED_TESTS${NC}"
echo ""

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}All tests passed!${NC}"
    exit 0
else
    echo -e "${RED}Some tests failed. Please check the output above.${NC}"
    exit 1
fi
