import { describe, expect, it } from "vitest";

import { getApiErrorMessage } from "./apiError";

describe("getApiErrorMessage", () => {
  it("prefers the 'detail' field from a ProblemDetails response", () => {
    const error = {
      response: { data: { detail: "El token expiró." } },
    };

    expect(getApiErrorMessage(error, "fallback")).toBe("El token expiró.");
  });

  it("falls back to the 'message' field when 'detail' is missing", () => {
    const error = {
      response: { data: { message: "Correo ya registrado." } },
    };

    expect(getApiErrorMessage(error, "fallback")).toBe(
      "Correo ya registrado."
    );
  });

  it("ignores a blank 'detail' and falls back to 'message'", () => {
    const error = {
      response: { data: { detail: "   ", message: "Correo ya registrado." } },
    };

    expect(getApiErrorMessage(error, "fallback")).toBe(
      "Correo ya registrado."
    );
  });

  it("returns the fallback when there is no response body", () => {
    expect(getApiErrorMessage(new Error("network error"), "fallback")).toBe(
      "fallback"
    );
  });

  it("returns the fallback when the response body has neither field", () => {
    const error = { response: { data: {} } };

    expect(getApiErrorMessage(error, "fallback")).toBe("fallback");
  });
});
