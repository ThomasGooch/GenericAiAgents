#!/bin/bash

# NuGet Publishing Script for Generic AI Agent System
# Run this script with your NuGet API key as an argument

if [ -z "$1" ]; then
    echo "❌ Usage: ./publish-to-nuget.sh YOUR_NUGET_API_KEY"
    echo "Get your API key from: https://www.nuget.org/account/apikeys"
    exit 1
fi

NUGET_API_KEY=$1
PACKAGE_DIR="./nupkg"

echo "🚀 Publishing Generic AI Agent System to NuGet.org..."
echo "📦 Publishing 11 packages..."
echo

# Publish packages in dependency order
PACKAGES=(
    "GenericAgents.Core.1.0.0.nupkg"
    "GenericAgents.Tools.1.0.0.nupkg" 
    "GenericAgents.Configuration.1.0.0.nupkg"
    "GenericAgents.Communication.1.0.0.nupkg"
    "GenericAgents.Security.1.0.0.nupkg"
    "GenericAgents.Observability.1.0.0.nupkg"
    "GenericAgents.Registry.1.0.0.nupkg"
    "GenericAgents.DI.1.0.0.nupkg"
    "GenericAgents.AI.1.0.0.nupkg"
    "GenericAgents.Orchestration.1.0.0.nupkg"
    "GenericAgents.Tools.Samples.1.0.0.nupkg"
)

SUCCESS_COUNT=0
FAILED_PACKAGES=()

for package in "${PACKAGES[@]}"; do
    echo "📤 Publishing $package..."
    
    if dotnet nuget push "$PACKAGE_DIR/$package" \
        --api-key "$NUGET_API_KEY" \
        --source https://api.nuget.org/v3/index.json \
        --skip-duplicate; then
        echo "✅ Successfully published $package"
        ((SUCCESS_COUNT++))
    else
        echo "❌ Failed to publish $package"
        FAILED_PACKAGES+=("$package")
    fi
    echo
done

echo "📊 Publishing Summary:"
echo "✅ Successful: $SUCCESS_COUNT/11"
echo "❌ Failed: ${#FAILED_PACKAGES[@]}/11"

if [ ${#FAILED_PACKAGES[@]} -gt 0 ]; then
    echo
    echo "Failed packages:"
    printf '%s\n' "${FAILED_PACKAGES[@]}"
    exit 1
fi

echo
echo "🎉 All packages published successfully!"
echo "🔗 View your packages at: https://www.nuget.org/profiles/YOUR_NUGET_PROFILE"
echo "📚 Packages will be available for installation in ~15 minutes"