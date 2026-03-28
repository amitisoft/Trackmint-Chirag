import { useMemo, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { api } from "../../services/api/client";
import { useAuthStore } from "../../store/auth-store";
import { useToast } from "../../components/common/ToastProvider";
import type { AuthResponse, ForgotPasswordResponse } from "../../types/models";

type AuthVariant = "login" | "register" | "forgot" | "reset";

function AuthLayout({
  variant,
  title,
  subtitle,
  children,
}: {
  variant: AuthVariant;
  title: string;
  subtitle: string;
  children: React.ReactNode;
}) {
  const location = useLocation();
  const isLogin = variant === "login";
  const isRegister = variant === "register";

  return (
    <div className="trackmint-auth">
      <div className="trackmint-auth__glow trackmint-auth__glow--left" />
      <div className="trackmint-auth__glow trackmint-auth__glow--right" />

      <section className="trackmint-auth__hero">
        <div className="trackmint-logo">
          <div className="trackmint-logo__mark">
            <span />
            <span />
          </div>
          <strong>TrackMint</strong>
        </div>

        <div className="trackmint-auth__copy">
          <h1>Track every rupee. Save smarter.</h1>
          <p>Manage expenses, budgets, and insights in one place.</p>
        </div>

        <div className="trackmint-auth__art">
          <div className="wallet-illustration">
            <div className="wallet-illustration__wallet" />
            <div className="wallet-illustration__card wallet-illustration__card--one" />
            <div className="wallet-illustration__card wallet-illustration__card--two" />
            <div className="wallet-illustration__bar wallet-illustration__bar--one" />
            <div className="wallet-illustration__bar wallet-illustration__bar--two" />
            <div className="wallet-illustration__bar wallet-illustration__bar--three" />
            <div className="wallet-illustration__pie" />
            <div className="wallet-illustration__coin wallet-illustration__coin--one">{"\u20B9"}</div>
            <div className="wallet-illustration__coin wallet-illustration__coin--two">{"\u20B9"}</div>
          </div>
        </div>

        <div className="trackmint-metrics">
          <div className="trackmint-metric">
            <span>Monthly Budget</span>
            <strong>{"\u20B9"}50,000</strong>
          </div>
          <div className="trackmint-metric">
            <span>Savings Rate</span>
            <strong>25%</strong>
          </div>
          <div className="trackmint-metric">
            <span>Recent Spend</span>
            <strong className="trackmint-metric__danger">-{"\u20B9"}8,200</strong>
          </div>
        </div>
      </section>

      <section className="trackmint-auth__panel">
        <div className="trackmint-auth__tabs">
          <Link to="/login" className={`trackmint-auth__tab ${isLogin ? "trackmint-auth__tab--active" : ""}`}>
            Sign In
          </Link>
          <Link to="/register" className={`trackmint-auth__tab ${isRegister ? "trackmint-auth__tab--active" : ""}`}>
            Sign Up
          </Link>
        </div>

        <div className="trackmint-auth__panel-copy">
          <h2>{title}</h2>
          <p>{subtitle}</p>
        </div>

        <div key={location.pathname}>{children}</div>
      </section>
    </div>
  );
}

const loginSchema = z.object({
  email: z.string().email("Enter a valid email"),
  password: z.string().min(8, "Password must be at least 8 characters"),
});

const registerSchema = loginSchema.extend({
  displayName: z.string().min(2, "Display name is required"),
});

const forgotSchema = z.object({
  email: z.string().email("Enter a valid email"),
});

const resetSchema = z.object({
  token: z.string().min(1, "Reset token is required"),
  password: z
    .string()
    .min(8, "Password must be at least 8 characters")
    .regex(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/, "Use uppercase, lowercase, and a number"),
});

function FormError({ message }: { message?: string }) {
  return <small>{message ?? ""}</small>;
}

export function LoginPage() {
  const navigate = useNavigate();
  const setSession = useAuthStore((state) => state.setSession);
  const { showToast } = useToast();
  const [rememberMe, setRememberMe] = useState(true);
  const form = useForm<z.infer<typeof loginSchema>>({ resolver: zodResolver(loginSchema), mode: "onChange" });

  const mutation = useMutation({
    mutationFn: async (values: z.infer<typeof loginSchema>) => {
      const response = await api.post<AuthResponse>("/auth/login", values);
      return response.data;
    },
    onSuccess: (session) => {
      setSession(session);
      showToast("Welcome back to TrackMint");
      navigate("/");
    },
    onError: (error: any) => {
      showToast(error?.response?.data?.message ?? "Sign in failed", "error");
    },
  });

  return (
    <AuthLayout variant="login" title="Welcome back to your finances" subtitle="Secure login with encrypted authentication.">
      <form className="trackmint-form" onSubmit={form.handleSubmit((values) => mutation.mutate(values))}>
        <label className="trackmint-field">
          <span>Email <span className="required-marker">*</span></span>
          <input type="email" placeholder="you@example.com" {...form.register("email")} />
          <FormError message={form.formState.errors.email?.message} />
        </label>

        <label className="trackmint-field">
          <span>Password <span className="required-marker">*</span></span>
          <input type="password" placeholder="Enter your password" {...form.register("password")} />
          <FormError message={form.formState.errors.password?.message} />
        </label>

        <div className="trackmint-form__meta">
          <label className="trackmint-checkbox">
            <input type="checkbox" checked={rememberMe} onChange={() => setRememberMe((current) => !current)} />
            <span>Remember me</span>
          </label>
          <Link to="/forgot-password">Forgot Password?</Link>
        </div>

        <button className="trackmint-submit" type="submit" disabled={!form.formState.isValid || mutation.isPending}>
          {mutation.isPending ? "Signing In..." : "Sign In"}
        </button>
      </form>
    </AuthLayout>
  );
}

export function RegisterPage() {
  const navigate = useNavigate();
  const setSession = useAuthStore((state) => state.setSession);
  const { showToast } = useToast();
  const form = useForm<z.infer<typeof registerSchema>>({ resolver: zodResolver(registerSchema), mode: "onChange" });

  const mutation = useMutation({
    mutationFn: async (values: z.infer<typeof registerSchema>) => {
      const response = await api.post<AuthResponse>("/auth/register", values);
      return response.data;
    },
    onSuccess: (session) => {
      setSession(session);
      showToast("TrackMint account created");
      navigate("/");
    },
    onError: (error: any) => {
      showToast(error?.response?.data?.message ?? "Sign up failed", "error");
    },
  });

  return (
    <AuthLayout variant="register" title="Create your TrackMint account" subtitle="Set up your workspace and start managing money with clarity.">
      <form className="trackmint-form" onSubmit={form.handleSubmit((values) => mutation.mutate(values))}>
        <label className="trackmint-field">
          <span>Display name <span className="required-marker">*</span></span>
          <input type="text" placeholder="Your name" {...form.register("displayName")} />
          <FormError message={form.formState.errors.displayName?.message} />
        </label>

        <label className="trackmint-field">
          <span>Email <span className="required-marker">*</span></span>
          <input type="email" placeholder="you@example.com" {...form.register("email")} />
          <FormError message={form.formState.errors.email?.message} />
        </label>

        <label className="trackmint-field">
          <span>Password <span className="required-marker">*</span></span>
          <input type="password" placeholder="Create a strong password" {...form.register("password")} />
          <FormError message={form.formState.errors.password?.message} />
        </label>

        <button className="trackmint-submit" type="submit" disabled={!form.formState.isValid || mutation.isPending}>
          {mutation.isPending ? "Creating..." : "Create Account"}
        </button>

        <div className="trackmint-form__footer">
          <span>Already have an account?</span>
          <Link to="/login">Sign In</Link>
        </div>
      </form>
    </AuthLayout>
  );
}

export function ForgotPasswordPage() {
  const { showToast } = useToast();
  const form = useForm<z.infer<typeof forgotSchema>>({ resolver: zodResolver(forgotSchema), mode: "onChange" });

  const mutation = useMutation({
    mutationFn: async (values: z.infer<typeof forgotSchema>) => {
      const response = await api.post<ForgotPasswordResponse>("/auth/forgot-password", values);
      return response.data;
    },
    onSuccess: (data) => {
      showToast(data.resetToken ? `Reset token: ${data.resetToken}` : data.message);
    },
    onError: (error: any) => {
      showToast(error?.response?.data?.message ?? "Could not generate reset token", "error");
    },
  });

  return (
    <AuthLayout variant="forgot" title="Reset your TrackMint access" subtitle="Generate a reset token and continue with a new password.">
      <form className="trackmint-form" onSubmit={form.handleSubmit((values) => mutation.mutate(values))}>
        <label className="trackmint-field">
          <span>Email <span className="required-marker">*</span></span>
          <input type="email" placeholder="you@example.com" {...form.register("email")} />
          <FormError message={form.formState.errors.email?.message} />
        </label>

        <button className="trackmint-submit" type="submit" disabled={!form.formState.isValid || mutation.isPending}>
          {mutation.isPending ? "Generating..." : "Generate Reset Token"}
        </button>

        <div className="trackmint-form__footer">
          <Link to="/reset-password">Go to reset form</Link>
          <Link to="/login">Back to login</Link>
        </div>
      </form>
    </AuthLayout>
  );
}

export function ResetPasswordPage() {
  const navigate = useNavigate();
  const { showToast } = useToast();
  const passwordHint = useMemo(() => "At least 8 characters with uppercase, lowercase, and a number", []);
  const form = useForm<z.infer<typeof resetSchema>>({ resolver: zodResolver(resetSchema), mode: "onChange" });

  const mutation = useMutation({
    mutationFn: async (values: z.infer<typeof resetSchema>) => {
      await api.post("/auth/reset-password", values);
    },
    onSuccess: () => {
      showToast("Password reset complete");
      navigate("/login");
    },
    onError: (error: any) => {
      showToast(error?.response?.data?.message ?? "Reset failed", "error");
    },
  });

  return (
    <AuthLayout variant="reset" title="Set a new password" subtitle="Paste your reset token and secure your TrackMint account again.">
      <form className="trackmint-form" onSubmit={form.handleSubmit((values) => mutation.mutate(values))}>
        <label className="trackmint-field">
          <span>Reset token <span className="required-marker">*</span></span>
          <input type="text" placeholder="Paste the generated token" {...form.register("token")} />
          <FormError message={form.formState.errors.token?.message} />
        </label>

        <label className="trackmint-field">
          <span>New password <span className="required-marker">*</span></span>
          <input type="password" placeholder="Create a new password" {...form.register("password")} />
          <small>{form.formState.errors.password?.message ?? passwordHint}</small>
        </label>

        <button className="trackmint-submit" type="submit" disabled={!form.formState.isValid || mutation.isPending}>
          {mutation.isPending ? "Resetting..." : "Reset Password"}
        </button>

        <div className="trackmint-form__footer">
          <Link to="/login">Back to login</Link>
        </div>
      </form>
    </AuthLayout>
  );
}
