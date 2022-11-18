// Copyright (c) Microsoft Corporation. All rights reserved.
// SPDX-License-Identifier: MIT

#include "ApiViewProcessor.hpp"
#include "AstNode.hpp"
#include "ProcessorImpl.hpp"
#include <nlohmann/json.hpp>
#include <string_view>

using namespace clang;
using namespace clang::tooling;

ApiViewProcessor::ApiViewProcessor(ApiViewProcessorOptions const& options)
    : m_processorImpl{std::make_unique<ApiViewProcessorImpl>(options)}
{
}

ApiViewProcessor::ApiViewProcessor(
    std::string_view const& pathToProcessor,
    std::string_view const apiViewSettings)
    : m_processorImpl{std::make_unique<ApiViewProcessorImpl>(pathToProcessor, apiViewSettings)}
{
}
ApiViewProcessor::ApiViewProcessor(
    std::string_view const& pathToProcessor,
    nlohmann::json const& apiViewSettings)
    : m_processorImpl{std::make_unique<ApiViewProcessorImpl>(pathToProcessor, apiViewSettings)}
{
}

ApiViewProcessor::~ApiViewProcessor() {}

int ApiViewProcessor::ProcessApiView() { return m_processorImpl->ProcessApiView(); }

int ApiViewProcessor::ProcessApiView(
    std::string_view const& sourceLocation,
    std::vector<std::string> const& additionalCompilerArguments,
    std::vector<std::string_view> const& filesToProcess)
{
  return m_processorImpl->ProcessApiView(
      sourceLocation, additionalCompilerArguments, filesToProcess);
}

std::unique_ptr<AzureClassesDatabase> const& ApiViewProcessor::GetClassesDatabase()
{
  return m_processorImpl->GetClassesDatabase();
}
