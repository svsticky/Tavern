#!/bin/bash
if ! awslocal s3api head-bucket --bucket profile-pictures 2>/dev/null; then
    awslocal s3 mb s3://profile-pictures
fi

if ! awslocal s3api head-bucket --bucket posters 2>/dev/null; then
    awslocal s3 mb s3://posters
fi

if ! awslocal s3api head-bucket --bucket group-pictures 2>/dev/null; then
    awslocal s3 mb s3://group-pictures
fi

if ! awslocal s3api head-bucket --bucket register-reason-icons 2>/dev/null; then
    awslocal s3 mb s3://register-reason-icons
fi

if ! awslocal s3api head-bucket --bucket register-slides 2>/dev/null; then
    awslocal s3 mb s3://register-slides
fi

if ! awslocal s3api head-bucket --bucket external-link-icons 2>/dev/null; then
    awslocal s3 mb s3://external-link-icons
fi