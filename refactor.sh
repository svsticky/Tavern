#!/bin/bash
cd /media/shared/Projects/Tavern

# Move interface file
mv Backend/Interfaces/IRegistrationDocumentRepository.cs Backend/Interfaces/IRegistrationDocumentService.cs

# Move domain service files
mkdir -p Backend/Services/Domain
if [ -d "Backend/Repositories" ]; then
    mv Backend/Repositories/* Backend/Services/Domain/
    rmdir Backend/Repositories
fi

# Rename moved files
for file in Backend/Services/Domain/*Repository.cs; do
    if [ -f "$file" ]; then
        mv "$file" "${file/Repository.cs/Service.cs}"
    fi
done

# Move test files
if [ -d "Backend.Tests/Repositories" ]; then
    mkdir -p Backend.Tests/Services/Domain
    mv Backend.Tests/Repositories/* Backend.Tests/Services/Domain/
    rmdir Backend.Tests/Repositories
    for file in Backend.Tests/Services/Domain/*RepositoryTests.cs; do
        if [ -f "$file" ]; then
            mv "$file" "${file/RepositoryTests.cs/ServiceTests.cs}"
        fi
    done
fi

# Do find and replace in all .cs files
find Backend Backend.Tests -type f -name "*.cs" -print0 | while IFS= read -r -d '' file; do
    sed -i 's/Backend\.Repositories/Backend.Services.Domain/g' "$file"
    sed -i 's/Repository/Service/g' "$file"
    sed -i 's/repository/service/g' "$file"
    sed -i 's/Repositories/Services/g' "$file"
    sed -i 's/repositories/services/g' "$file"
done
