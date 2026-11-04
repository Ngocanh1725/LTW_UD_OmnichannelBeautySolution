# Test E2E Order Flow for Session B4
$ErrorActionPreference = "Stop"
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

Write-Host "=== STEP 1: Access Homepage ===" -ForegroundColor Cyan
$homeRes = Invoke-WebRequest -Uri "http://localhost:5116/" -WebSession $session -UseBasicParsing
Write-Host "Homepage Status: $($homeRes.StatusCode), Cookies: $($session.Cookies.Count)" -ForegroundColor Green

Write-Host "`n=== STEP 2: Add to Cart (Product ID 8) ===" -ForegroundColor Cyan
$addPayload = '{"productId": 8, "quantity": 1}'
$addRes = Invoke-RestMethod -Uri "http://localhost:5116/Cart/AddToCart" -Method Post -Body $addPayload -ContentType "application/json" -WebSession $session
Write-Host "AddToCart: Success=$($addRes.success), Count=$($addRes.cartCount), SubTotal=$($addRes.subTotalFormatted), Shipping=$($addRes.shippingFee), Remaining=$($addRes.freeShippingRemainingFormatted)" -ForegroundColor Green

Write-Host "`n=== STEP 3: Access /Cart ===" -ForegroundColor Cyan
$cartRes = Invoke-WebRequest -Uri "http://localhost:5116/Cart" -WebSession $session -UseBasicParsing
Write-Host "Cart Page Status: $($cartRes.StatusCode)" -ForegroundColor Green

$cartSummary = Invoke-RestMethod -Uri "http://localhost:5116/Cart/GetCartSummary" -Method Get -WebSession $session
Write-Host "CartSummary: TotalItems=$($cartSummary.totalItems), SubTotal=$($cartSummary.subTotalFormatted), Shipping=$($cartSummary.shippingFeeFormatted), FinalTotal=$($cartSummary.finalTotalFormatted)" -ForegroundColor Green

Write-Host "`n=== STEP 4: Access /Checkout ===" -ForegroundColor Cyan
$checkoutRes = Invoke-WebRequest -Uri "http://localhost:5116/Checkout" -WebSession $session -UseBasicParsing
Write-Host "Checkout Page Status: $($checkoutRes.StatusCode)" -ForegroundColor Green

Write-Host "`n=== STEP 5: Submit PlaceOrder COD ===" -ForegroundColor Cyan
$orderPayload = @{
    recipientName = "Nguyen Van A"
    recipientPhone = "0901234567"
    recipientEmail = "nguyenvana@example.com"
    shippingAddress = "123 Le Loi, Ben Nghe, Quan 1, TP.HCM"
    shippingMethod = "STANDARD"
    paymentMethod = 4
    notes = "Test Phien B4 Clean Code"
}

$placeOrderRes = Invoke-WebRequest -Uri "http://localhost:5116/Checkout/PlaceOrder" -Method Post -Body $orderPayload -WebSession $session -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue

$location = $placeOrderRes.Headers["Location"]
Write-Host "PlaceOrder Status: $($placeOrderRes.StatusCode), Redirect Location: $location" -ForegroundColor Yellow

$orderId = ""
if ($location -match "orderId=([^&]+)") {
    $orderId = $Matches[1]
} elseif ($location -match "/Checkout/OrderSuccess/([^?]+)") {
    $orderId = $Matches[1]
}

if ($orderId) {
    Write-Host "`n=== STEP 6: Verify OrderSuccess page for order: $orderId ===" -ForegroundColor Cyan
    $successUrl = "http://localhost:5116/Checkout/OrderSuccess?orderId=$orderId"
    $successRes = Invoke-WebRequest -Uri $successUrl -WebSession $session -UseBasicParsing
    Write-Host "OrderSuccess Page Status: $($successRes.StatusCode)" -ForegroundColor Green
    if ($successRes.Content.Contains($orderId)) {
        Write-Host "SUCCESS CONFIRMED: Order $orderId was placed and displayed correctly on OrderSuccess view!" -ForegroundColor Green
    } else {
        Write-Host "OrderSuccess loaded successfully with status $($successRes.StatusCode)" -ForegroundColor Green
    }
} else {
    Write-Host "No redirect location found. Checking content:"
    Write-Host $placeOrderRes.Content
}
